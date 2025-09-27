using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SpellProjectile : NetworkBehaviour {
    [Header("References")]
    public Rigidbody rb;

    public Collider coll;
    public Renderer renderer;
    public ParticleSystem ps;

    private IProjectileMovement movement;
    private IProjectileDamage damage;
    private IProjectileImpact impact;
    private IProjectileLifetime lifetime;

    public SpellData spellData;

    public void Initialize(SpellData data) {
        spellData = data;

        Debug.Log($"[SpellProjectile] Игрок {OwnerClientId} выпустил {spellData.name}");

        if (!IsServer) return;
        movement = spellData.isHoming
            ? new HomingMovement(this, rb, spellData)
            : new StraightMovement(this, rb, spellData);
        movement.Initialize();

        if (spellData.isDOT)
            damage = new DotDamage(this, spellData);
        else if (spellData.hasAreaEffect)
            damage = new AreaDamage(this, spellData);
        else
            damage = new DirectDamage(this, spellData);

        impact = new ImpactEffect(this, spellData);

        lifetime = new ProjectileLifetime(this, spellData);
        lifetime.Initialize();
    }

    private void Update() {
        if (!IsServer) return;

        movement.Tick();
        lifetime.Tick();
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer || other.isTrigger) return;

        damage.OnHit(other);
        impact.OnImpact(other);

        if (!spellData.piercing)
            lifetime.Destroy();
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer || other.isTrigger) return;

        damage.OnStay(other);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnImpactServerRpc(int spellId, Vector3 position, Quaternion quaternion, ulong ownerId) {
        var spell = SpellDatabase.Instance.GetSpell(spellId);
        var go = Instantiate(spell.impactPrefab, position, quaternion);
        if (go.TryGetComponent<NetworkObject>(out var netObj)) {
            netObj.SpawnWithOwnership(ownerId);
        } else {
            throw new Exception($"[{spell.name}] impact prefab must be a NetworkObject");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyProjectileServerRpc(ulong objectId) {
        DestroyProjectileClientRpc(objectId);
        StartCoroutine(WaitAndDestroy(objectId));
    }

    private IEnumerator WaitAndDestroy(ulong objectId) {
        yield return new WaitForSeconds(0.5f);

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            yield break;

        netObj.Despawn();
        Destroy(netObj.gameObject);
    }

    [ClientRpc]
    private void DestroyProjectileClientRpc(ulong objectId) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;
        var projectile = netObj.GetComponent<SpellProjectile>();

        // Отключаем видимые компоненты
        if (projectile.coll != null) projectile.coll.enabled = false;
        if (projectile.renderer != null) projectile.renderer.enabled = false;
        if (projectile.rb != null) projectile.rb.isKinematic = true;
        if (projectile.ps != null) {
            var emission = projectile.ps.emission;
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