using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Settings/Movement Settings")]
public class MovementSettings : ScriptableObject {
    [Header("Movement")]
    public float staminaRestore = 0.1f;

    public float staminaUsage = 0.5f;
    public bool canRun = true;
    public KeyCode runningKey = KeyCode.LeftShift;
    public float flySpeedMultiplier = 0.5f;

    [Header("Jumping")]

    public float jumpDelay = 0.5f;
    public float jumpCooldown = 1f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Climbing")]
    public bool canClimb = true;
    public float climbMaxHeight = 1.2f;
    public float climbMaxDistance = 1f;
    public float climbDuration = 0.2f;
    public float climbSurfaceForwardOffsetMin = 0.15f;
    public float climbSurfaceForwardOffsetMax = 0.35f;
    public float climbSurfaceForwardOffsetStep = 0.05f;
    public float climbSurfaceUpOffset = 0.05f;
    public float climbCeilingCheckExtra = 0.05f;
    public LayerMask climbCollisionMask = ~0;

    [Header("Gravity")]
    public float gravity = -9.81f;

    public float fallGravityMultiplier = 2.5f;
}