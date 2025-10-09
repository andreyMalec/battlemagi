using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Data")]
public class SpellData : ScriptableObject {
    public int id;
    public string name;
    [Multiline(5)]
    public string description;
    public string nameRu;
    public string[] spellWords;
    public string[] spellWordsRu;
    public Texture2D bookImage;
    public GameObject spellInHandPrefab;
    public GameObject spellBurstPrefab;
    public GameObject mainSpellPrefab;
    public GameObject impactPrefab;
    public DamageSoundType damageSound;

    public int invocationIndex;

    public bool clearInHandBeforeAnim = false;

    [Header("Damage")]
    public float baseDamage = 10f;

    public bool canSelfDamage = true;
    public bool useParticleCollision = false;
    [HideIf("hasAreaEffect")] public bool isDOT = false;
    [HideIf("isDOT")] public bool hasAreaEffect = true;
    [ShowIf("hasAreaEffect")] public float areaRadius = 5f;

    [Header("Projectile")]
    public bool isProjectile = true;

    [ShowIf("isProjectile")] public float lifeTime = 20f;
    [ShowIf("isProjectile")] public bool piercing = false;

    [HideIf("isBeam")] [ShowIf("isProjectile")]
    public float baseSpeed = 20f;

    [ShowIf("isProjectile")] public int projCount = 1;
    [ShowIf("isProjectile")] public float multiProjDelay = 0.2f;
    [ShowIf("isProjectile")] public SpawnMode spawnMode = SpawnMode.Direct;

    [HideIf("isHoming")] [ShowIf("isProjectile")]
    public bool isBeam = false;

    [Header("Homing")]
    [HideIf("isBeam")] [ShowIf("isProjectile")]
    public bool isHoming = false;

    [ShowIf(EConditionOperator.And, "isProjectile", "isHoming")]
    public float homingRadius = 10f;

    [ShowIf(EConditionOperator.And, "isProjectile", "isHoming")]
    public float homingStrength = 1f;

    [Header("Channeling")]
    public bool isChanneling = false;

    [ShowIf("isChanneling")] public float channelDuration = 3f;

#if UNITY_EDITOR
    private void OnValidate() {
        if (isDOT)
            hasAreaEffect = false;
        if (hasAreaEffect)
            isDOT = false;

        if (isBeam)
            isHoming = false;
        if (isHoming)
            isBeam = false;
    }
#endif
}