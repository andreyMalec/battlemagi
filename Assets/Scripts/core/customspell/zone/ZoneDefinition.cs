using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Zone Spell", menuName = "Spells/Zone Definition")]
public class ZoneDefinition : ScriptableObject, IValidate {
    public SpellZonePrefabId prefabId;
    public ZoneShapeType shapeType;

    public SpellMovement moveType;

    [ShowIf("_canMove")] public float moveSpeed;
    [ShowIf("_transformLinear")] public bool moveAlongGround;
    [ShowIf(EConditionOperator.And, "_transformLinear", "moveAlongGround")] public float groundOffset = 0.1f;

    [ShowIf("_canMove")] public bool enableMaxDistance;
    [ShowIf("enableMaxDistance")] public float maxDistance = 20f;
    [ShowIf(EConditionOperator.And, "enableMaxDistance", "_canReturnToCaster")] public bool returnToCaster;

    [Header("LookAtPoint")]
    [ShowIf("_transformLookAtPoint")] public float lookAtMaxDistance = 50f;

    [ShowIf("_transformLookAtPoint")] public LayerMask lookAtRayMask = ~0;

    [Header("FollowCaster")]
    [ShowIf("_transformFollowCaster")] public FollowCasterTarget followTarget;

    [Header("Spiral")]
    [ShowIf("_transformSpiral")] public float angularSpeed;

    [ShowIf("_transformSpiral")] public float spiralRadius = 0.5f;
    [ShowIf("_transformSpiral")] public SpiralAxis spiralAxis = SpiralAxis.Forward;

    [Header("Accelerated")]
    [ShowIf("_transformAccelerated")] public float acceleration;

    [Header("Homing")]
    [ShowIf("_canHoming")] public bool enableHoming;

    [ShowIf("enableHoming")] public LayerMask obstacleMask = ~0;
    [ShowIf("enableHoming")] public float homingRadius = 10f;
    [ShowIf("enableHoming")] public float homingMaxTurnDegrees = 25f;
    [ShowIf("enableHoming")] public float homingSlerp = 0.35f;

    [Header("SquashStretch")]
    public bool enableSquashStretch;

    [ShowIf("enableSquashStretch")] public float stretchAmplitude = 0.2f;
    [ShowIf("enableSquashStretch")] public float stretchFrequency = 8f;
    [ShowIf("enableSquashStretch")] public float stretchDamping = 0f;

    [Header("Special")]
    public bool teleportOnSpawn;

    public bool destroyIncomingSpells;
    public bool impassableForEnemies;

    [Header("Spawned Spells")]
    public SpellDefinition atStepDistanceSpawn;

    [ShowIf("enableMaxDistance")] public SpellDefinition atMaxDistanceSpawn;
    [ShowIf("destroyIncomingSpells")] public SpellDefinition onEnemySpellDestroyedSpawn;

    [ShowIf("spawnAtStep")] public float spawnStep = 10f;
    public SpellDefinition onLifetimeEndSpawn;
    public SpellDefinition onLifetimeHalfSpawn;

    private bool _canMove = false;
    private bool _transformLinear = false;
    private bool _transformSpiral = false;
    private bool _transformAccelerated = false;
    private bool _transformLookAtPoint = false;
    private bool _transformFollowCaster = false;
    private bool _canHoming = false;
    private bool _canReturnToCaster = false;
    [ShowIf("false")] [HideInInspector] public bool spawnAtStep = false;

    public void Validate() {
        _canMove = moveType is not SpellMovement.Static;
        _transformLinear = moveType is SpellMovement.Linear;
        _transformSpiral = moveType is SpellMovement.Spiral;
        _transformAccelerated = moveType is SpellMovement.Accelerated;
        _transformLookAtPoint = moveType is SpellMovement.LookAtPoint;
        _transformFollowCaster = moveType is SpellMovement.FollowCaster;
        _canHoming = moveType is SpellMovement.Linear or SpellMovement.Spiral or SpellMovement.Accelerated;
        _canReturnToCaster = moveType is SpellMovement.Linear or SpellMovement.Spiral or SpellMovement.Accelerated;
        spawnAtStep = atStepDistanceSpawn != null;

        if (!_canReturnToCaster)
            returnToCaster = false;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Validate();
    }
#endif
}