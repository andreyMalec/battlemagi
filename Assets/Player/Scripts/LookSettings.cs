using UnityEngine;

[CreateAssetMenu(fileName = "LookSettings", menuName = "Settings/Look Settings")]
public class LookSettings : ScriptableObject {
    [Header("Camera Control")] public float sensitivity = 2f;
    public float smoothing = 1.5f;

    [Header("Rotation Clamp")] public float yMin = -90f;
    public float yMax = 90f;

    [Header("Head Following")] public float positionSmoothTime = 0.01f;
    public float rotationSmoothTime = 0.2f;
    public Vector3 offset = Vector3.zero;
    public Vector3 rotation = Vector3.zero;
}