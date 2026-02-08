using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Zone Spell", menuName = "Spells/Zone Definition")]
public class ZoneDefinition : ScriptableObject {
    public SpellZonePrefabId prefabId;

    public SpellTransform moveType;

    [ShowIf("_canMove")] public float moveSpeed;

    [ShowIf("_canMove")] public bool enableMaxDistance;
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