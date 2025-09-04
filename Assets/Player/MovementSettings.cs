using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Settings/Movement Settings")]
public class MovementSettings : ScriptableObject {
    [Header("Movement")] public float speed = 4f;
    public float runSpeed = 8f;
    public bool canRun = true;
    public KeyCode runningKey = KeyCode.LeftShift;
    public float flySpeedMultiplier = 0.5f;

    [Header("Jumping")] public float jumpStrength = 2f;
    public float jumpDelay = 0.5f;
    public float jumpCooldown = 1f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Gravity")] public float gravity = -9.81f;
    public float fallGravityMultiplier = 2.5f;
    public float maxFallSpeed = -25f;
}