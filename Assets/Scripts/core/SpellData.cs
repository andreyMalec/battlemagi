using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Data")]
public class SpellData : ScriptableObject {
    public string spellName;
    public GameObject spellBurstPrefab;
    public GameObject mainSpellPrefab;
    public GameObject impactPrefab;

    public int invocationIndex;

    public float castTime = 2f;

    public float baseDamage = 10f;
    public bool hasAreaEffect = true;
    public float areaRadius = 5f;

    [Header("Projectile")] public float lifeTime = 20f;
    public bool piercing = false;
    public float baseSpeed = 20f;
    public float homingStrength = 1f;

    [Header("Shake")] public bool shakeEnabled = true;
    public float shakeStrengthBurst = 0.05f;
    public float shakeStrengthImpact = 0.05f;
    public float shakeDurationBurst = 0.2f;
    public float shakeDurationImpact = 0.2f;

    public Color spellColor = Color.white;
    public bool spellTracking = false;
    public bool spawnOnGround = false;

    public GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation) {
        if (prefab != null) {
            GameObject obj = Instantiate(prefab, position, rotation);
            ApplyColorToParticles(obj);
            return obj;
        }

        return null;
    }

    private void ApplyColorToParticles(GameObject spellObject) {
        if (spellObject == null || spellColor == Color.white) return;
        ParticleSystem[] particleSystems = spellObject.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems) {
            var mainModule = ps.main;
            mainModule.startColor = spellColor;
        }
    }
}