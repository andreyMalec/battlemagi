using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Beam Spell", menuName = "Spells/Beam Definition")]
public class BeamDefinition : ScriptableObject {
    public SpellBeamPrefabId prefabId;

    public SpellTransform moveType;

    public float beamMaxLength = 15f;

    [ShowIf("_canMove")] public float moveSpeed;

    [ShowIf("_canMove")] public bool enableMaxDistance;
    [ShowIf("enableMaxDistance")] public float maxDistance = 20f;

    [Header("LookAtPoint")]
    [ShowIf("_transformLookAtPoint")] public float lookAtMaxDistance = 50f;

    [ShowIf("_transformLookAtPoint")] public LayerMask lookAtRayMask = ~0;

    [Header("FollowCaster")]
    [ShowIf("_transformFollowCaster")] public FollowCasterTarget followTarget;

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
    private bool _transformSpiral = false;
    private bool _transformLookAtPoint = false;
    private bool _transformFollowCaster = false;
    [HideInInspector] public bool spawnAtStep = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _canMove = moveType is not SpellTransform.Static;
        _transformSpiral = moveType is SpellTransform.Spiral;
        _transformLookAtPoint = moveType is SpellTransform.LookAtPoint;
        _transformFollowCaster = moveType is SpellTransform.FollowCaster;
        spawnAtStep = atStepDistanceSpawn != null;
    }
#endif
}