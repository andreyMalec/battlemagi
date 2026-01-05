using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Data")]
public class SpellData : ScriptableObject {
    public int id;
    public string name;
    public bool enabled = true;
    public string[] spellWords;
    public string[] spellWordsRu;
    public float manaCost = 50;
    public Texture2D bookImage;
    public GameObject spellInHandPrefab;
    public GameObject spellBurstPrefab;
    public GameObject mainSpellPrefab;
    public GameObject impactPrefab;
    public ImpactEffect[] impactEffects;
    public StatusEffectData[] buffs;
    public DamageSoundType damageSound;

    public int castWaitingIndex = 0;
    public int invocationIndex;

    public bool clearInHandBeforeAnim = false;
    public bool previewMainInHand = false;
    public bool disableWhileCarrying = false;

    public int echoCount = 0;
    public int instanceLimit = 0;

    public float lifeTime = 20f;

    [Header("Damage")]
    public float baseDamage = 10f;

    public float structureDamageMultiplier = 1f;

    public bool canSelfDamage = true;
    public bool useParticleCollision = false;
    public bool isDOT = false;
    [Min(0.001f)] [ShowIf("isDOT")] public float tickInterval = 1;
    public bool hasAreaEffect = true;
    [ShowIf("hasAreaEffect")] public float areaRadius = 5f;

    public float knockbackForce = 0f;

    [Header("Projectile")]
    public bool isProjectile = true;

    public bool piercing = false;

    [HideIf("isBeam")] public float baseSpeed = 20f;

    public int projCount = 1;
    public float multiProjDelay = 0.2f;
    public SpawnMode spawnMode = SpawnMode.Direct;
    public bool useAlternativeSpawnMode = false;
    [ShowIf("useAlternativeSpawnMode")] public SpawnMode alternativeSpawnMode = SpawnMode.Direct;

    [ShowIf("_isForward")] public Vector3 forwardStep = Vector3.zero;
    [ShowIf("_isForward")] public Vector3 forwardAngle = Vector3.zero;
    [ShowIf("_isArc")] public float arcAngleStep = 15f;

    [ShowIf("_isRaycast")] public float raycastMaxDistance = 50f;

    [HideIf("isHoming")] [ShowIf("isProjectile")]
    public bool isBeam = false;

    [Header("Homing")]
    [HideIf("isBeam")] [ShowIf("isProjectile")]
    public bool isHoming = false;

    [ShowIf("isHoming")] public float homingRadius = 10f;

    [ShowIf("isHoming")] public float homingStrength = 1f;

    [Header("Channeling")]
    public bool isChanneling = false;

    [ShowIf("isChanneling")] public float channelDuration = 0f;
    [ShowIf("isChanneling")] public bool isCharging = false;

    private bool _isArc = false;
    private bool _isRaycast = false;
    private bool _isForward = false;
#if UNITY_EDITOR
    private void OnValidate() {
        _isArc = spawnMode is SpawnMode.Arc or SpawnMode.GroundPointArc ||
                 alternativeSpawnMode is SpawnMode.Arc or SpawnMode.GroundPointArc;
        _isRaycast = spawnMode is SpawnMode.GroundPoint or SpawnMode.GroundPointArc or SpawnMode.HitScan
                         or SpawnMode.DirectDown ||
                     alternativeSpawnMode is SpawnMode.GroundPoint or SpawnMode.GroundPointArc or SpawnMode.HitScan
                         or SpawnMode.DirectDown;
        _isForward = spawnMode is SpawnMode.GroundPointForward ||
                     alternativeSpawnMode is SpawnMode.GroundPointForward;

        if (isBeam)
            isHoming = false;
        if (isHoming)
            isBeam = false;
    }
#endif
}