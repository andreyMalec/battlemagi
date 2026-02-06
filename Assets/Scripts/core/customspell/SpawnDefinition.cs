using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spawn Strategy", menuName = "Spells/Spawn Definition")]
public class SpawnDefinition : ScriptableObject {
    public Preview preview;
    public int instanceCount = 1;
    public int instanceLimit = 0;
    [ShowIf("_isMultiInstance")] public float multiInstanceDelay = 0.2f;
    [ShowIf("_isDelayed")] public DelayOrigin delayOrigin;
    public SpawnMode spawnMode = SpawnMode.Direct;
    public bool useAlternativeSpawnMode = false;
    [ShowIf("useAlternativeSpawnMode")] public SpawnMode alternativeSpawnMode = SpawnMode.Direct;

    [ShowIf("_isForward")] public Vector3 forwardStep = Vector3.zero;
    [ShowIf("_isForward")] public Vector3 forwardAngle = Vector3.zero;
    [ShowIf("_isArc")] public float arcAngleStep = 15f;

    [ShowIf("_isRaycast")] public float raycastMaxDistance = 50f;

    private bool _isArc = false;
    private bool _isRaycast = false;
    private bool _isForward = false;

    private bool _isMultiInstance = false;
    private bool _isDelayed = false;

#if UNITY_EDITOR
    private void OnValidate() {
        _isMultiInstance = instanceCount > 1;
        _isDelayed = multiInstanceDelay > 0;
        _isArc = spawnMode is SpawnMode.Arc or SpawnMode.GroundPointArc ||
                 alternativeSpawnMode is SpawnMode.Arc or SpawnMode.GroundPointArc;
        _isRaycast = spawnMode is SpawnMode.GroundPoint or SpawnMode.GroundPointArc or SpawnMode.HitScan
                         or SpawnMode.DirectDown ||
                     alternativeSpawnMode is SpawnMode.GroundPoint or SpawnMode.GroundPointArc or SpawnMode.HitScan
                         or SpawnMode.DirectDown;
        _isForward = spawnMode is SpawnMode.GroundPointForward ||
                     alternativeSpawnMode is SpawnMode.GroundPointForward;
    }
#endif
}