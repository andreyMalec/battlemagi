using UnityEngine;

/// <summary>
/// Simple configurable rotation behaviour.
/// Allows independent rotation speed on each axis and choice of space.
/// </summary>
[ExecuteAlways]
public class RotationOverTime : MonoBehaviour {
    [Header("Rotation Speed (degrees per second)")]
    [Tooltip("Rotation speed in degrees per second for each axis.")]
    public Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    [Header("Settings")]
    [Tooltip("Rotate in local or world space.")]
    public Space rotationSpace = Space.Self;

    [Tooltip("Enable or disable rotation at runtime.")]
    public bool isActive = true;

    private void Update() {
        if (!isActive) return;

        // delta rotation this frame
        Vector3 deltaRotation = rotationSpeed * Time.deltaTime;

        transform.Rotate(deltaRotation, rotationSpace);
    }

    /// <summary>
    /// Instantly reverses rotation direction on all axes.
    /// </summary>
    public void Reverse() {
        rotationSpeed = -rotationSpeed;
    }

    /// <summary>
    /// Set rotation speed for a single axis.
    /// </summary>
    public void SetAxisSpeed(Vector3 newSpeed) {
        rotationSpeed = newSpeed;
    }
}