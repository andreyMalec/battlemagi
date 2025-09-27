using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Data")]
public class SpellData : ScriptableObject {
    public int id;
    public string name;
    public string nameRu;
    public string[] spellWords;
    public string[] spellWordsRu;
    public GameObject spellInHandPrefab;
    public GameObject mainSpellPrefab;
    public GameObject impactPrefab;
    public DamageSoundType damageSound;

    public int invocationIndex;

    public float castTime = 2f;

    public bool isDOT = false;
    public float baseDamage = 10f;
    public bool hasAreaEffect = true;
    public float areaRadius = 5f;

    [Header("Projectile")] public float lifeTime = 20f;
    public bool piercing = false;
    public float baseSpeed = 20f;
    public float homingRadius = 10f;
    public float homingStrength = 1f;
    public int projCount = 1;

    public bool spellTracking = false;
    public bool spawnOnGround = false;
    public bool canSelfDamage = true;
}