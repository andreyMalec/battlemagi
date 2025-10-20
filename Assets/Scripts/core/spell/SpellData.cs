using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Data")]
public class SpellData : ScriptableObject {
    public int id;
    public string name;
    [Multiline(5)] public string description;
    public string nameRu;
    public string[] spellWords;
    public string[] spellWordsRu;
    public float manaCost = 50;
    public Texture2D bookImage;
    public GameObject spellInHandPrefab;
    public GameObject spellBurstPrefab;
    public GameObject mainSpellPrefab;
    public GameObject impactPrefab;
    public DamageSoundType damageSound;

    public int castWaitingIndex = 0;
    public int invocationIndex;

    public bool clearInHandBeforeAnim = false;
    public bool previewMainInHand = false;

    public int echoCount = 0;

    public float lifeTime = 20f;

    [Header("Damage")]
    public float baseDamage = 10f;

    public bool canSelfDamage = true;
    public bool useParticleCollision = false;
    [HideIf("hasAreaEffect")] public bool isDOT = false;
    [HideIf("isDOT")] public bool hasAreaEffect = true;
    [ShowIf("hasAreaEffect")] public float areaRadius = 5f;

    [Header("Projectile")]
    public bool isProjectile = true;

    [ShowIf("isProjectile")] public bool piercing = false;

    [ShowIf("isProjectile")] [HideIf("isBeam")]
    public float baseSpeed = 20f;

    [ShowIf("isProjectile")] public int projCount = 1;
    [ShowIf("isProjectile")] public float multiProjDelay = 0.2f;
    [ShowIf("isProjectile")] public SpawnMode spawnMode = SpawnMode.Direct;

    [ShowIf(EConditionOperator.And, "isProjectile", "_isArc")]
    public float arcAngleStep = 15f;

    [ShowIf(EConditionOperator.And, "isProjectile", "_isRaycast")]
    public float raycastMaxDistance = 50f;

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

    private bool _isArc = false;
    private bool _isRaycast = false;
#if UNITY_EDITOR
    private void OnValidate() {
        _isArc = spawnMode is SpawnMode.Arc or SpawnMode.GroundPointArc;
        _isRaycast = spawnMode is SpawnMode.GroundPoint or SpawnMode.GroundPointArc or SpawnMode.HitScan;

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