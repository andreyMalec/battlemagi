using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spawn Strategy", menuName = "Spells/Spawn Definition")]
public class SpawnDefinition : ScriptableObject {
    public Preview preview;
    public int instanceCount = 1;
    public int instanceLimit = 0;
    [ShowIf("_isMultiInstance")] public float multiInstanceDelay = 0.2f;

    [ShowIf(EConditionOperator.And, "_isDelayed", "_respectDelayOrigin")]
    public DelayOrigin delayOrigin;

    public SpawnMode spawnMode = SpawnMode.Direct;
    public bool useAlternativeSpawnMode = false;
    [ShowIf("useAlternativeSpawnMode")] public SpawnMode alternativeSpawnMode = SpawnMode.Direct;

    [ShowIf("_isForward")] public float forwardStep = 3;
    [ShowIf("_isArc")] public float arcAngleStep = 15f;

    [ShowIf("_isRaycast")] public float raycastMaxDistance = 50f;

    private bool _isArc = false;
    private bool _isRaycast = false;
    private bool _isForward = false;

    private bool _isMultiInstance = false;
    private bool _isDelayed = false;
    private bool _respectDelayOrigin = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _isMultiInstance = instanceCount > 1;
        _isDelayed = multiInstanceDelay > 0;
        _respectDelayOrigin = RespectOrigin(spawnMode);
        _isArc = IsArc(spawnMode) || IsArc(alternativeSpawnMode);
        _isRaycast = IsRay(spawnMode) || IsRay(alternativeSpawnMode);
        _isForward = IsForward(spawnMode) || IsForward(alternativeSpawnMode);
    }

    private static bool RespectOrigin(SpawnMode spawnMode) {
        return spawnMode is SpawnMode.Direct or SpawnMode.DirectDown or SpawnMode.GroundPoint;
    }

    private static bool IsArc(SpawnMode spawnMode) {
        return spawnMode is SpawnMode.Arc or SpawnMode.GroundPointArc;
    }

    private static bool IsRay(SpawnMode spawnMode) {
        return spawnMode is SpawnMode.GroundPoint or SpawnMode.GroundPointArc
            or SpawnMode.DirectDown or SpawnMode.DirectDownForward;
    }

    private static bool IsForward(SpawnMode spawnMode) {
        return spawnMode is SpawnMode.GroundPointForward or SpawnMode.DirectDownForward;
    }
#endif
}