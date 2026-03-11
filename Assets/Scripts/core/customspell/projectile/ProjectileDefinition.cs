using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile Spell", menuName = "Spells/Projectile Definition")]
public class ProjectileDefinition : ScriptableObject, IValidate {
    public SpellProjectilePrefabId prefabId;

    public SpellMovement moveType;

    [ShowIf("_canMove")] public float moveSpeed;
    [ShowIf("_canMove")] public bool enableMaxDistance;
    [ShowIf("enableMaxDistance")] public float maxDistance = 20f;
    public bool enableGravity;
    [ShowIf("enableGravity")] public Vector3 gravity = new(0, -9.81f, 0);

    [Header("LookAtPoint")]
    [ShowIf("_transformLookAtPoint")] public float lookAtMaxDistance = 50f;

    [ShowIf("_transformLookAtPoint")] public LayerMask lookAtRayMask = ~0;

    [Header("Spiral")]
    [ShowIf("_transformSpiral")] public float angularSpeed = 5;

    [ShowIf("_transformSpiral")] public float spiralRadius = 0.5f;
    [ShowIf("_transformSpiral")] public SpiralAxis spiralAxis = SpiralAxis.Forward;

    [Header("SquashStretch")]
    public bool enableSquashStretch;

    [ShowIf("enableSquashStretch")] public float stretchAmplitude = 0.2f;
    [ShowIf("enableSquashStretch")] public float stretchFrequency = 8f;
    [ShowIf("enableSquashStretch")] public float stretchDamping = 0f;

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

    [Header("Spawned Spells")]
    public SpellDefinition onHitSpawn;

    [ShowIf("enableMaxDistance")] public SpellDefinition atMaxDistanceSpawn;
    public SpellDefinition atStepDistanceSpawn;
    [ShowIf("spawnAtStep")] public float spawnStep = 10f;
    public SpellDefinition onLifetimeEndSpawn;
    public SpellDefinition onLifetimeHalfSpawn;

    private bool _canMove = false;
    private bool _transformSpiral = false;
    private bool _transformLookAtPoint = false;
    private bool _transformFollowCaster = false;
    [ShowIf("false")] [HideInInspector] public bool spawnAtStep = false;

    public void Validate() {
        _canMove = moveType is not SpellMovement.Static;
        _transformSpiral = moveType is SpellMovement.Spiral;
        _transformLookAtPoint = moveType is SpellMovement.LookAtPoint;
        _transformFollowCaster = moveType is SpellMovement.FollowCaster;
        spawnAtStep = atStepDistanceSpawn != null;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}