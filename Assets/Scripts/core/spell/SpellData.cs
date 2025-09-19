using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Data")]
public class SpellData : ScriptableObject {
    public int id;
    public string name;
    public string nameRu;
    public string[] spellWords;
    public string[] spellWordsRu;
    public GameObject spellInHandPrefab;
    public GameObject spellBurstPrefab;
    public GameObject mainSpellPrefab;
    public GameObject impactPrefab;
    public AudioClip damageSound;

    public int invocationIndex;

    public float castTime = 2f;

    public bool isDOT = false;
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

    public bool spellTracking = false;
    public bool spawnOnGround = false;
    public bool canSelfDamage = true;
}