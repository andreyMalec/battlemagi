using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SpellLifetime))]
public class BaseSpell : NetworkBehaviour {
    private int terrainLayer;

    [Header("References")]
    public Rigidbody rb;

    private ISpellMovement movement;
    private ISpellDamage damage;
    private SpellLifetime lifetime;

    public SpellData spellData;
    [HideInInspector] public float damageMultiplier = 1;
    private bool movementAuthority;

    // debounce: prevent multiple colliders of the same owner from triggering repeatedly
    private readonly Dictionary<ulong, float> lastTriggerTimes = new Dictionary<ulong, float>();
    private float triggerDebounce = 0.02f;

    private void Awake() {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        foreach (var singletonConnectedClient in NetworkManager.Singleton.ConnectedClients) {
            var player = singletonConnectedClient.Value.PlayerObject;
            ps?.trigger.AddCollider(player.GetComponent<Collider>());
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var mode = GetComponent<NetworkTransform>().AuthorityMode;
        movementAuthority = (mode == NetworkTransform.AuthorityModes.Owner && IsOwner) ||
                            (mode == NetworkTransform.AuthorityModes.Server && IsServer);
        lifetime = GetComponent<SpellLifetime>();
        terrainLayer = LayerMask.NameToLayer("Terrain");
    }

    public void Initialize(SpellData data, float damageMulti, int index) {
        spellData = data;
        damageMultiplier = damageMulti;

        Debug.Log($"[SpellProjectile] Игрок {OwnerClientId} выпустил {spellData.name}[{NetworkObjectId}]");

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

        lifetime.Initialize(this, spellData);

        if (data.buffs != null && data.buffs.Length > 0) {
            var player = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject;
            if (player != null && player.TryGetComponent<StatusEffectManager>(out var manager)) {
                foreach (var effect in data.buffs) {
                    manager.AddEffect(OwnerClientId, effect);
                }
            }
        }

        if (spellData.baseSpeed > 0) {
            if (Physics.Raycast(transform.position - transform.forward * 2, transform.forward, out var hit, 2f,
                    1 << terrainLayer)) {
                Debug.Log($"[BaseSpell] Initial raycast hit terrain at {hit.point}");
                OnTriggerStay(hit.collider);
            } else if (Physics.Raycast(transform.position, transform.forward, out var hit2, 2f,
                           1 << terrainLayer)) {
                Debug.Log($"[BaseSpell] Initial raycast hit2 terrain at {hit2.point}");
                transform.position = hit2.point;
                OnTriggerStay(hit2.collider);
            }
        }
    }

    private void Update() {
        if (movementAuthority) {
            movement?.Tick();
        }

        if (!IsServer) return;
        var hit = damage.Update();
        if (hit)
            ApplyImpact();
    }

    private void OnCollisionEnter(Collision other) {
        OnTriggerEnter(other.collider);
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer || other.isTrigger || !lifetime.IsAlive) return;

        ulong? hitOwner = null;

        if (other.TryGetComponent<ForceField>(out var field)) {
            // союзный купол? игнор
            if (TeamManager.Instance.AreAllies(OwnerClientId, field.OwnerClientId))
                return;
        } else if (other.TryGetComponent<ChildCollider>(out _)) {
            var player = other.GetComponentInParent<Player>();
            if (!spellData.canSelfDamage &&
                TeamManager.Instance.AreAllies(OwnerClientId, player.OwnerClientId))
                return;
            hitOwner = player.OwnerClientId;
        } else if (other.TryGetComponent<Player>(out var player)) {
            if (!spellData.canSelfDamage &&
                TeamManager.Instance.AreAllies(OwnerClientId, player.OwnerClientId))
                return;
            hitOwner = player.OwnerClientId;
        } else if (other.TryGetComponent<BaseSpell>(out var spell)) {
            if (spell.spellData.isProjectile)
                return;
        }

        if (hitOwner.HasValue) {
            if (lastTriggerTimes.TryGetValue(hitOwner.Value, out var last) && Time.time - last < triggerDebounce)
                return;
            lastTriggerTimes[hitOwner.Value] = Time.time;
        }

        damage.OnEnter(other);
        if (!spellData.isDOT)
            ApplyImpact();

        if (!spellData.piercing)
            lifetime.Destroy();
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer || other.isTrigger || !lifetime.IsAlive) return;
        if (other.gameObject.layer != terrainLayer) return;
        OnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other) {
        if (!IsServer) return;
        damage.OnExit(other);
    }

    private void OnParticleCollision(GameObject other) {
        if (!IsServer) return;

        if (spellData.useParticleCollision) {
            OnTriggerEnter(other.gameObject.GetComponent<Collider>());
        }
    }

    private void OnParticleTrigger() {
        if (!spellData.useParticleCollision)
            return;

        ParticleSystem ps = GetComponent<ParticleSystem>();
        List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
        List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
        int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
        int numIn = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
        Debug.Log(
            $"[BaseSpell] OnParticleTrigger called for spell {spellData.name}; Number of particles Enter: {numEnter}, Inside: {numIn}");
        for (int i = 0; i < numEnter; i++) {
            ParticleSystem.Particle p = enter[i];
            var c = Physics.OverlapSphere(p.position, spellData.areaRadius);
            Debug.Log($" [BaseSpell] Enter Particle at {p.position} overlaps {c.Length} colliders");
            for (int j = 0; j < c.Length; j++) {
                OnTriggerEnter(c[j]);
            }
        }

        for (int i = 0; i < numIn; i++) {
            ParticleSystem.Particle p = inside[i];
            var c = Physics.OverlapSphere(p.position, spellData.areaRadius);
            Debug.Log($" [BaseSpell] Inside Particle at {p.position} overlaps {c.Length} colliders");
            for (int j = 0; j < c.Length; j++) {
                OnTriggerEnter(c[j]);
            }
        }
    }

    private void ApplyImpact() {
        if (spellData.impactEffects.Length == 0) return;
        foreach (var impact in spellData.impactEffects) {
            impact.OnImpact(this, spellData);
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        ClearVisual();
        if (IsServer) {
            if (spellData != null)
                SpellInstanceLimiter.Unregister(OwnerClientId, spellData, NetworkObject);
            else
                SpellInstanceLimiter.UnregisterByObject(NetworkObject);
        }
    }

    [ClientRpc]
    public void PreDespawnCleanupClientRpc() {
        ClearVisual();
    }

    private void ClearVisual() {
        foreach (var c in GetComponentsInChildren<Collider>()) {
            c.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Renderer>()) {
            c.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<ParticleSystem>()) {
            var emission = c.emission;
            emission.rateOverTime = 0f;
            c.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (rb != null) rb.isKinematic = true;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        rb = GetComponent<Rigidbody>();
    }
#endif

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