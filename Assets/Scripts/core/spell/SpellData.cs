using NaughtyAttributes;
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

    public bool spawnOnGround = false;
    [Header("Damage")]
    public float baseDamage = 10f;

    public bool canSelfDamage = true;
    public bool isDOT = false;
    public bool hasAreaEffect = true;
    [ShowIf("hasAreaEffect")] public float areaRadius = 5f;

    [Header("Projectile")]
    public bool isProjectile = true;

    [ShowIf("isProjectile")] public float lifeTime = 20f;

    [ShowIf("isProjectile")] public bool piercing = false;
    [ShowIf("isProjectile")] public float baseSpeed = 20f;
    [ShowIf("isProjectile")] public int projCount = 1;

    [Header("Homing")]
    [ShowIf("isProjectile")] public bool isHoming = false;

    [ShowIf(EConditionOperator.And, "isProjectile", "isHoming")]
    public float homingRadius = 10f;

    [ShowIf(EConditionOperator.And, "isProjectile", "isHoming")]
    public float homingStrength = 1f;
}