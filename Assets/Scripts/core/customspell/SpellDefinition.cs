using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject {
    public CoreType coreType;
    public SpawnDefinition spawn;
    public GameObject mainPrefab;

    public float lifetime;
    public SpellTransform moveType;

    public LayerMask defaultRaycast = ~0;

    [Header("Projectile")]
    public float projectileSpeed;

    public bool enableGravity;
    [ShowIf("enableGravity")] public Vector3 gravity = new(0, -9.81f, 0);
    public bool enableMaxDistance;
    [ShowIf("enableMaxDistance")] public float maxDistance = 20f;

    [Header("LookAtPoint")]
    [ShowIf("_transformLookAtPoint")] public float lookAtMaxDistance = 50f;

    [ShowIf("_transformLookAtPoint")] public LayerMask lookAtRayMask = ~0;

    [Header("FollowCaster")]
    [ShowIf("_transformFollowCaster")] public FollowCasterTarget followTarget;

    [Header("Spiral")]
    [ShowIf("_transformSpiral")] public float angularSpeed;

    [ShowIf("_transformSpiral")] public float spiralRadius = 0.5f;
    [ShowIf("_transformSpiral")] public SpiralAxis spiralAxis = SpiralAxis.Forward;

    [Header("SquashStretch")]
    public bool enableSquashStretch;

    [ShowIf("enableSquashStretch")] public float stretchAmplitude = 0.2f;
    [ShowIf("enableSquashStretch")] public float stretchFrequency = 8f;
    [ShowIf("enableSquashStretch")] public float stretchDamping = 0f;

    [Header("Beam")]
    public float beamMaxLength = 15f;

    [Header("Bounce")]
    public bool enableBounce;

    [ShowIf("enableBounce")] public int maxBounces = 3;
    [ShowIf("enableBounce")] public float bounceSpeedMultiplier = 0.9f;

    [Header("Pierce")]
    public bool enablePierce;

    [ShowIf("enablePierce")] public int maxPierces = 1;

    [Header("Fork")]
    public bool enableFork;

    [ShowIf("enableFork")] public int forkCount = 3;
    [ShowIf("enableFork")] public float forkSpreadAngle = 35f;

    [Header("Zone")]
    public float zoneRadius = 1;

    [Header("Spawned Spells")]
    public SpellDefinition onHitSpawnZone;
    public SpellDefinition atMaxDistanceSpawn;
    public SpellDefinition onLifetimeEndSpawn;

    private bool _transformSpiral = false;
    private bool _transformLookAtPoint = false;
    private bool _transformFollowCaster = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _transformSpiral = moveType is SpellTransform.Spiral;
        _transformLookAtPoint = moveType is SpellTransform.LookAtPoint;
        _transformFollowCaster = moveType is SpellTransform.FollowCaster;
    }
#endif
}