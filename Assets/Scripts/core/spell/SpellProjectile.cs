using System;
using System.Collections;
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
        Debug.Log($"[{gameObject.name}] OnTriggerEnter 0");
        if (other.isTrigger) return;
        if (!IsServer) return;
        Debug.Log($"[{gameObject.name}] OnTriggerEnter 1");

        HandleImpact(other);
        Debug.Log($"[{gameObject.name}] OnTriggerEnter HandleImpact");

        if (!spellData.piercing)
            DestroyProjectileServerRpc(NetworkObjectId);
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

    private void HandleImpact(Collider other) {
        // Apply damage
        if (other.TryGetComponent<Damageable>(out var damageable)) {
            var damage = spellData.baseDamage;
            Debug.Log(
                $"[{gameObject.name}] Прямое попадание в игрока {other.GetComponent<NetworkObject>().OwnerClientId}");
            damageable.TakeDamage(damage);
        }
        Debug.Log($"[{gameObject.name}] HandleImpact 0");

        // Area effect
        if (spellData.hasAreaEffect)
            ApplyAreaEffect(spellData.canSelfDamage ? null : OwnerClientId);
        Debug.Log($"[{gameObject.name}] HandleImpact 1");

        // Spawn impact effect
        if (spellData.impactPrefab != null)
            SpawnImpactClientRpc(spellData.id, transform.position);
        Debug.Log($"[{gameObject.name}] HandleImpact 2");
    }

    [ClientRpc]
    private void SpawnImpactClientRpc(int spellId, Vector3 position) {
        var spell = SpellDatabase.Instance.GetSpell(spellId);

        Instantiate(spell.impactPrefab, position, Quaternion.identity);
    }

    private void ApplyAreaEffect(ulong? excludeClientId) {
        var hits = Physics.OverlapSphere(transform.position, spellData.areaRadius);
        foreach (var hit in hits) {
            if (hit.TryGetComponent<Damageable>(out var player)) {
                var netObj = hit.GetComponent<NetworkObject>();
                Debug.Log($"[{gameObject.name}] Взрыв задел игрока {netObj.OwnerClientId}");
                var distance = Vector3.Distance(transform.position, hit.transform.position);
                var damageMultiplier = 1f - distance / spellData.areaRadius;

                if (excludeClientId.HasValue && netObj.OwnerClientId == excludeClientId.Value) continue;
                var damageable = hit.GetComponent<Damageable>();
                damageable.TakeDamage(spellData.baseDamage * damageMultiplier);
            }
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