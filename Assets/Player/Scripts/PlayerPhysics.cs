using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPhysics : MonoBehaviour {
    private MovementSettings _settings;
    private GroundCheck _groundCheck;

    private CharacterController _controller;

    private float _velocityY; // managed by gravity/jumps

    private Vector3 _externalVelocity;

    [Tooltip("Generic decay (per second)")] [SerializeField]
    private float _impulseDamping = 5f;

    [Tooltip("Extra horizontal decay when grounded")] [SerializeField]
    private float _groundFriction = 8f;

    // Jump anti-snap window
    private const float NoSnapDuration = 0.08f; // seconds after jump to ignore ground snap
    private float _noSnapTimer;
    private bool _groundedForPhysics;

    private void Awake() {
        _controller = GetComponent<CharacterController>();
    }

    public void Configure(MovementSettings settings, GroundCheck groundCheck) {
        _settings = settings;
        _groundCheck = groundCheck;
    }

    public void MoveWithGravity(Vector3 desiredWorldVelocity) {
        float dt = Time.deltaTime;
        if (_noSnapTimer > 0f) _noSnapTimer -= dt;

        // Ignore ground snap for a brief window after jump
        _groundedForPhysics = _groundCheck.isGrounded && _noSnapTimer <= 0f;

        ApplyGravity(dt);

        // Combine desired movement (from input), external impulses, and vertical velocity
        Vector3 combinedVelocity = desiredWorldVelocity + _externalVelocity + Vector3.up * _velocityY;
        Vector3 motion = combinedVelocity * dt;

        // Move and capture collisions to clamp external velocity if blocked
        if (!_controller.enabled) return;
        CollisionFlags flags = _controller.Move(motion);

        // Decay external velocity over time
        DecayExternalVelocity(dt, flags);
    }

    // Instant impulse interpreted as delta-velocity (units ~= m/s)
    public void ApplyImpulse(Vector3 impulse) {
        _externalVelocity += impulse;
    }

    // Jump by setting vertical velocity derived from settings gravity
    public void Jump(float jumpStrength) {
        // v = sqrt(strength * -2 * g)
        _velocityY = Mathf.Sqrt(jumpStrength * -2f * _settings.gravity);
        _noSnapTimer = NoSnapDuration;
    }

    private void ApplyGravity(float dt) {
        if (_groundedForPhysics && _velocityY < 0f) {
            // small downward bias to keep grounded only when descending
            _velocityY = -2f;
        } else {
            float gravityMul = _velocityY < 0f ? _settings.fallGravityMultiplier : 1f;
            _velocityY += _settings.gravity * gravityMul * dt;
        }
    }

    private void DecayExternalVelocity(float dt, CollisionFlags flags) {
        // Clamp external velocity when hitting environment
        if ((flags & CollisionFlags.Sides) != 0) {
            // Cancel horizontal push if we hit a wall
            _externalVelocity.x = 0f;
            _externalVelocity.z = 0f;
        }

        if ((flags & CollisionFlags.Above) != 0 && _externalVelocity.y > 0f) {
            _externalVelocity.y = 0f;
        }

        // Horizontal damping: generic + extra ground friction
        Vector3 horiz = new Vector3(_externalVelocity.x, 0f, _externalVelocity.z);
        float horizDamp = _impulseDamping + (_groundedForPhysics ? _groundFriction : 0f);
        float horizReduce = horizDamp * dt;
        if (horiz.magnitude > 0f) {
            float newMag = Mathf.Max(0f, horiz.magnitude - horizReduce);
            if (newMag == 0f) {
                horiz = Vector3.zero;
            } else {
                horiz = horiz.normalized * newMag;
            }
        }

        // Vertical external velocity: kill on ground, damp in air
        float y = _externalVelocity.y;
        if (_groundedForPhysics) {
            y = 0f;
        } else if (y != 0f) {
            float sign = Mathf.Sign(y);
            y -= sign * _impulseDamping * dt;
            if (Mathf.Sign(y) != sign) y = 0f;
        }

        _externalVelocity = new Vector3(horiz.x, y, horiz.z);
    }
}