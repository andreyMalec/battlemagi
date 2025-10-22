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

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var mode = GetComponent<NetworkTransform>().AuthorityMode;
        movementAuthority = (mode == NetworkTransform.AuthorityModes.Owner && IsOwner) ||
                            (mode == NetworkTransform.AuthorityModes.Server && IsServer);
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
        DestroySpellClientRpc(objectId);
        StartCoroutine(WaitAndDestroy(objectId));
    }

    private IEnumerator WaitAndDestroy(ulong objectId) {
        yield return new WaitForSeconds(0.5f);

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            yield break;

        netObj.Despawn();
        if (netObj.gameObject != null)
            Destroy(netObj.gameObject);
    }

    [ClientRpc]
    private void DestroySpellClientRpc(ulong objectId) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;
        var spell = netObj.GetComponent<BaseSpell>();

        // Отключаем видимые компоненты
        if (spell.coll != null) spell.coll.enabled = false;
        if (spell.renderer != null) spell.renderer.enabled = false;
        if (spell.rb != null) spell.rb.isKinematic = true;
        if (spell.ps != null) {
            var emission = spell.ps.emission;
            emission.rateOverTime = 0f;
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