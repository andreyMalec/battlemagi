using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class BaseSpell : NetworkBehaviour {
    public event Action<float> LifetimePercent;

    [Header("References")]
    public Rigidbody rb;

    public Collider coll;
    public Renderer renderer;
    public ParticleSystem ps;

    private ISpellMovement movement;
    private ISpellDamage damage;
    private ISpellLifetime lifetime;

    public SpellData spellData;
    [HideInInspector] public float damageMultiplier = 1;
    private bool movementAuthority;

    // Failsafe against stuck projectiles
    [Header("Failsafe")]
    [SerializeField] private float failSafeLifetimeSec = 20f;

    [Tooltip("Enable server-side stuck detection by position (disabled by default for static spells like walls)")]
    [SerializeField]
    private bool enableStuckDetection = false;

    [SerializeField] private float stuckPosEpsilon = 0.02f; // meters
    [SerializeField] private float stuckTimeSec = 2f;

    private float _serverSpawnTime;
    private float _lastMovedTime;
    private Vector3 _lastServerPos;
    private bool _despawning;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var mode = GetComponent<NetworkTransform>().AuthorityMode;
        movementAuthority = (mode == NetworkTransform.AuthorityModes.Owner && IsOwner) ||
                            (mode == NetworkTransform.AuthorityModes.Server && IsServer);
        if (IsServer) {
            _serverSpawnTime = Time.time;
            _lastMovedTime = _serverSpawnTime;
            _lastServerPos = transform.position;
        }
    }

    public virtual void Initialize(SpellData data, float damageMulti, int index) {
        spellData = data;
        damageMultiplier = damageMulti;

        Debug.Log($"[SpellProjectile] Игрок {OwnerClientId} выпустил {spellData.name}");

        if (movementAuthority || IsServer) {
            if (spellData.isBeam)
                movement = new BeamMovement(this, rb, spellData, index);
            else if (spellData.isHoming)
                movement = new HomingMovement(this, rb, spellData);
            else
                movement = new StraightMovement(this, rb, spellData);
            movement.Initialize();
        }

        if (!IsServer) return;

        if (spellData.isDOT)
            damage = new DotDamage(this, spellData);
        else if (spellData.hasAreaEffect)
            damage = new AreaDamage(this, spellData);
        else
            damage = new DirectDamage(this, spellData);

        lifetime = new SpellLifetime(this, spellData);
        lifetime.Initialize();
    }

    private void Update() {
        if (movementAuthority) {
            movement?.Tick();
        }

        if (!IsServer) return;

        LifetimePercent?.Invoke(lifetime.Tick());

        if (!_despawning && failSafeLifetimeSec > 0f && Time.time - _serverSpawnTime > failSafeLifetimeSec) {
            ForceDespawn($"Failsafe lifetime exceeded ({failSafeLifetimeSec}s)");
            return;
        }

        if (!_despawning && enableStuckDetection && !spellData.isBeam) {
            var cur = transform.position;
            if ((cur - _lastServerPos).sqrMagnitude > stuckPosEpsilon * stuckPosEpsilon) {
                _lastMovedTime = Time.time;
                _lastServerPos = cur;
            } else if (Time.time - _lastMovedTime > stuckTimeSec) {
                ForceDespawn($"Stuck detected: no movement > {stuckTimeSec}s (eps {stuckPosEpsilon})");
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer || other.isTrigger) return;
        if (other.gameObject.TryGetComponent<ForceField>(out var field)) {
            // союзный купол? игнор
            if (TeamManager.Instance.AreAllies(field.OwnerClientId, OwnerClientId))
                return;
        }

        damage.OnHit(other);
        OnHit(other);

        if (!spellData.piercing)
            lifetime.Destroy();
    }

    protected virtual void OnHit(Collider other) {
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer) return;

        damage.OnStay(other);
    }

    private void OnParticleCollision(GameObject other) {
        if (!IsServer) return;

        if (spellData.useParticleCollision) {
            OnTriggerEnter(other.gameObject.GetComponent<Collider>());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroySpellServerRpc(ulong objectId) {
        ForceDespawn();
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (ps != null) {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (coll != null) coll.enabled = false;
        if (renderer != null) renderer.enabled = false;
        if (rb != null) rb.isKinematic = true;
    }

    private void ForceDespawn(string reason = null) {
        if (_despawning) return;
        _despawning = true;

        if (!string.IsNullOrEmpty(reason))
            Debug.LogWarning($"[BaseSpell] Despawning {name}: {reason}");

        // Pre-cleanup visuals on all clients to avoid hanging FX, then despawn
        PreDespawnCleanupClientRpc();

        if (NetworkObject != null && NetworkObject.IsSpawned) {
            NetworkObject.Despawn(true);
        } else {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void PreDespawnCleanupClientRpc() {
        if (coll != null) coll.enabled = false;
        if (renderer != null) renderer.enabled = false;
        if (rb != null) rb.isKinematic = true;
        if (ps != null) {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDrawGizmos() {
        if (spellData == null) return;
        if (spellData.hasAreaEffect) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spellData.areaRadius);
        }

        if (spellData.isHoming) {
            Gizmos.color = Color.deepSkyBlue;
            Gizmos.DrawWireSphere(transform.position, spellData.homingRadius);
        }
    }
}