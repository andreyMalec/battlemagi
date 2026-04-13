using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Beam Spell", menuName = "Spells/Beam Definition")]
public class BeamDefinition : ScriptableObject, IValidate {
    public SpellBeamPrefabId prefabId;

    public BeamShapeType shapeType;

    [ShowIf("_shapeStraight")] public float beamMaxLength = 15f;

    [ShowIf("_shapeCone")] public float coneRadius;
    [ShowIf("_shapeCone")] public float coneAngle = 25f;
    [ShowIf("_shapeCone")] public float coneLength = 15f;

    public SpellMovement moveType;

    [ShowIf("_canMove")] public float moveSpeed;

    [ShowIf("_canMove")] public bool enableMaxDistance;
    [ShowIf("enableMaxDistance")] public float maxDistance = 20f;

    [Header("LookAtPoint")]
    [ShowIf("_transformLookAtPoint")] public float lookAtMaxDistance = 50f;

    [ShowIf("_transformLookAtPoint")] public LayerMask lookAtRayMask = ~0;

    [Header("FollowCaster")]
    [ShowIf("_transformFollowCaster")] public FollowCasterTarget followTarget;

    [Header("Accelerated")]
    [ShowIf("_transformAccelerated")] public float acceleration;

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
    public SpellDefinition onHitSpawnZone;

    [ShowIf("enableMaxDistance")] public SpellDefinition atMaxDistanceSpawn;
    public SpellDefinition atStepDistanceSpawn;
    [ShowIf("spawnAtStep")] public float spawnStep = 10f;
    public SpellDefinition onLifetimeEndSpawn;
    public SpellDefinition onLifetimeHalfSpawn;

    private bool _canMove = false;
    private bool _shapeStraight = true;
    private bool _shapeCone = false;
    private bool _transformSpiral = false;
    private bool _transformAccelerated = false;
    private bool _transformLookAtPoint = false;
    private bool _transformFollowCaster = false;
    [ShowIf("false")] [HideInInspector] public bool spawnAtStep = false;

    public float MaxLength => shapeType switch {
        BeamShapeType.Cone => coneLength,
        _ => beamMaxLength
    };

    public void Validate() {
        _shapeStraight = shapeType is BeamShapeType.Straight;
        _shapeCone = shapeType is BeamShapeType.Cone;
        _canMove = moveType is not SpellMovement.Static;
        _transformSpiral = moveType is SpellMovement.Spiral;
        _transformAccelerated = moveType is SpellMovement.Accelerated;
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