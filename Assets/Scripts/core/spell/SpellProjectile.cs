using UnityEngine;

public class SpellProjectile : MonoBehaviour {
    [Header("References")] public Rigidbody rb;

    public Collider coll;
    public Renderer renderer;
    public ParticleSystem ps;
    private float currentLifeTime;

    private SpellData spellData;

    private void Update() {
        currentLifeTime += Time.deltaTime;

        if (currentLifeTime >= spellData.lifeTime) 
            DestroyProjectile();

        // Homing behavior
        if (spellData.spellTracking) 
            ApplyHoming();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.isTrigger)
            return;

        HandleImpact(other);

        if (!spellData.piercing)
            DestroyProjectile();
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
            damageable.TakeDamage(damage);
        }

        // Area effect
        if (spellData.hasAreaEffect) 
            ApplyAreaEffect();

        // Spawn impact effect
        if (spellData.impactPrefab != null)
            Instantiate(spellData.impactPrefab, transform.position, Quaternion.identity);
    }

    private void ApplyAreaEffect() {
        var hits = Physics.OverlapSphere(transform.position, spellData.areaRadius);
        foreach (var hit in hits)
            if (hit.TryGetComponent<Damageable>(out var damageable)) {
                var distance = Vector3.Distance(transform.position, hit.transform.position);
                var damageMultiplier = 1f - distance / spellData.areaRadius;
                damageable.TakeDamage(spellData.baseDamage * damageMultiplier);
            }
    }

    private void DestroyProjectile() {
        // Отключаем видимые компоненты
        if (coll != null) coll.enabled = false;
        if (renderer != null) renderer.enabled = false;
        if (rb != null) rb.isKinematic = true;
        if (ps != null) {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
        }

        // Уничтожаем через 3 секунды чтобы эффекты успели проиграться
        Destroy(gameObject, 3f);
    }
}