using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SpellLifetime))]
public class BaseSpell : NetworkBehaviour {
    [Header("References")]
    public Rigidbody rb;

    private ISpellMovement movement;
    private ISpellDamage damage;
    private SpellLifetime lifetime;

    public SpellData spellData;
    [HideInInspector] public float damageMultiplier = 1;
    private bool movementAuthority;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var mode = GetComponent<NetworkTransform>().AuthorityMode;
        movementAuthority = (mode == NetworkTransform.AuthorityModes.Owner && IsOwner) ||
                            (mode == NetworkTransform.AuthorityModes.Server && IsServer);
        lifetime = GetComponent<SpellLifetime>();
    }

    public virtual void Initialize(SpellData data, float damageMulti, int index) {
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

        if (data.buffs == null || data.buffs.Length == 0) return;
        var player = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject;
        if (player == null || !player.TryGetComponent<StatusEffectManager>(out var manager)) return;

        foreach (var effect in data.buffs) {
            manager.AddEffect(OwnerClientId, effect);
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

    private void OnTriggerEnter(Collider other) {
        if (!IsServer || other.isTrigger) return;
        if (other.gameObject.TryGetComponent<ForceField>(out var field)) {
            // союзный купол? игнор
            if (TeamManager.Instance.AreAllies(field.OwnerClientId, OwnerClientId))
                return;
        }

        damage.OnEnter(other);
        if (!spellData.isDOT)
            ApplyImpact();

        if (!spellData.piercing)
            lifetime.Destroy();
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