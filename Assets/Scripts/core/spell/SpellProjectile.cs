using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpellProjectile : NetworkBehaviour {
    [Header("References")] public Rigidbody rb;

    public Collider coll;
    public Renderer renderer;
    public ParticleSystem ps;
    private float currentLifeTime;

    private SpellData spellData;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        Debug.Log($"[SpellProjectile] Игрок {OwnerClientId} выпустил {gameObject.name}");
    }

    private void Update() {
        if (!IsServer) return;

        currentLifeTime += Time.deltaTime;

        if (currentLifeTime >= spellData.lifeTime)
            DestroyProjectileServerRpc(NetworkObjectId);

        // Homing behavior
        if (spellData.spellTracking)
            ApplyHoming();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.isTrigger) return;
        if (!IsServer) return;

        HandleImpact(other);

        if (!spellData.piercing)
            DestroyProjectileServerRpc(NetworkObjectId);
    }

    private void OnTriggerStay(Collider other) {
        if (!spellData.isDOT) return;

        ApplyDamage(Array.Empty<ulong>(), other);
    }

    public void Initialize(SpellData data) {
        spellData = data;

        // Apply initial force
        var speed = spellData.baseSpeed;
        rb.linearVelocity = transform.forward * speed;

        currentLifeTime = 0f;
    }

    private void ApplyHoming() {
        // Simple homing implementation
        var nearby = Physics.OverlapSphere(transform.position, 10f);
        foreach (var col in nearby)
            if (col.CompareTag("Enemy")) {
                var direction = (col.transform.position - transform.position).normalized;
                rb.linearVelocity = Vector3.Lerp(
                    rb.linearVelocity.normalized,
                    direction,
                    spellData.homingStrength * Time.deltaTime
                ) * rb.linearVelocity.magnitude;
                break;
            }
    }

    /**
     * @returns clientId
     */
    private ulong ApplyDamage(ulong[] excludeClients, Collider other, bool applyDistanceMultiplier = false) {
        if (!other.TryGetComponent<Damageable>(out var damageable)) return ulong.MaxValue;
        var netObj = other.GetComponent<NetworkObject>();
        if (!spellData.canSelfDamage && OwnerClientId == netObj.OwnerClientId) return ulong.MaxValue;
        if (excludeClients.Contains(netObj.OwnerClientId)) return ulong.MaxValue;

        if (applyDistanceMultiplier) {
            Debug.Log($"[{gameObject.name}] Взрыв задел игрока {netObj.OwnerClientId}");
            var distance = Vector3.Distance(transform.position, other.transform.position);
            var damageMultiplier = 1f - distance / spellData.areaRadius;

            damageable.TakeDamage(spellData.baseDamage * damageMultiplier, spellData.damageSound);
        } else {
            Debug.Log($"[{gameObject.name}] Прямое попадание в игрока {netObj.OwnerClientId}");
            damageable.TakeDamage(spellData.baseDamage, spellData.damageSound);
        }

        return netObj.OwnerClientId;
    }

    private void HandleImpact(Collider other) {
        if (spellData.isDOT) return;
        // Apply damage
        var excludeClients = new[] { ulong.MaxValue, ulong.MaxValue };
        excludeClients[1] = ApplyDamage(excludeClients, other);

        excludeClients[0] = spellData.canSelfDamage ? ulong.MaxValue : OwnerClientId;
        // Area effect
        if (spellData.hasAreaEffect)
            ApplyAreaEffect(excludeClients);

        // Spawn impact effect
        if (spellData.impactPrefab != null)
            SpawnImpactClientRpc(spellData.id, transform.position, OwnerClientId);
    }

    [ClientRpc]
    private void SpawnImpactClientRpc(int spellId, Vector3 position, ulong ownerId) {
        var spell = SpellDatabase.Instance.GetSpell(spellId);

        var go = Instantiate(spell.impactPrefab, position, Quaternion.identity);
        if (IsServer && go.TryGetComponent<NetworkObject>(out var netObj)) {
            netObj.SpawnWithOwnership(ownerId);
        }
    }

    private void ApplyAreaEffect(ulong[] excludeClients) {
        Debug.Log($"[{gameObject.name}] ApplyAreaEffect exclude {string.Join(", ", excludeClients)}");
        var hits = Physics.OverlapSphere(transform.position, spellData.areaRadius);
        foreach (var hit in hits) {
            ApplyDamage(excludeClients, hit);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyProjectileServerRpc(ulong objectId) {
        DestroyProjectileClientRpc(objectId);
        StartCoroutine(WaitAndDestroy(objectId));
    }

    private IEnumerator WaitAndDestroy(ulong objectId) {
        yield return new WaitForSeconds(2);

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
}