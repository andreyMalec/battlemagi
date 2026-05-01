using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPhysics : MonoBehaviour {
    private class PointForceState {
        public Vector3 point;
        public float forcePerSecond;
        public float remaining;
        public SpellKnockbackVectorMode vectorMode;
        public float upBias;
    }

    private class VelocitySourceState {
        public Vector3 velocity;
        public float remaining;
    }

    private MovementSettings _settings;
    private GroundCheck _groundCheck;

    private CharacterController _controller;

    private float _velocityY; // managed by gravity/jumps

    private Vector3 _externalVelocity;
    private readonly Dictionary<int, PointForceState> _pointForces = new();
    private readonly List<int> _pointForcesToRemove = new();
    private readonly Dictionary<int, VelocitySourceState> _velocitySources = new();
    private readonly List<int> _velocitySourcesToRemove = new();

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

        var breaksGroundSnap = ApplyPointForces(dt);
        var sourceVelocity = ApplyVelocitySources(dt, out var sourceBreaksGroundSnap);
        breaksGroundSnap |= sourceBreaksGroundSnap;
        if (breaksGroundSnap && _velocityY < 0f) {
            _velocityY = 0f;
        }

        _groundedForPhysics = _groundCheck.isGrounded && _noSnapTimer <= 0f && !breaksGroundSnap && _externalVelocity.y <= 0f && sourceVelocity.y <= 0f;

        ApplyGravity(dt);

        Vector3 combinedVelocity = desiredWorldVelocity + _externalVelocity + sourceVelocity + Vector3.up * _velocityY;
        Vector3 motion = combinedVelocity * dt;

        if (!_controller.enabled) return;
        CollisionFlags flags = _controller.Move(motion);

        DecayExternalVelocity(dt, flags);
    }

    public void ApplyImpulse(Vector3 impulse) {
        _externalVelocity += impulse;
    }

    public void ApplyForce(Vector3 force, float dt) {
        if (dt <= 0f) return;
        _externalVelocity += force * dt;
    }

    public void SetPointForce(
        int id,
        Vector3 point,
        float forcePerSecond,
        float duration,
        SpellKnockbackVectorMode vectorMode,
        float upBias
    ) {
        if (forcePerSecond <= 0f || duration <= 0f) return;

        if (_pointForces.TryGetValue(id, out var state)) {
            state.point = point;
            state.forcePerSecond = forcePerSecond;
            state.remaining = duration;
            state.vectorMode = vectorMode;
            state.upBias = upBias;
            return;
        }

        _pointForces.Add(id, new PointForceState {
            point = point,
            forcePerSecond = forcePerSecond,
            remaining = duration,
            vectorMode = vectorMode,
            upBias = upBias,
        });
    }

    public void SetVelocitySource(int id, Vector3 velocity, float duration) {
        if (duration <= 0f || velocity.sqrMagnitude < 0.0001f) {
            _velocitySources.Remove(id);
            return;
        }

        if (_velocitySources.TryGetValue(id, out var state)) {
            state.velocity = velocity;
            state.remaining = duration;
            return;
        }

        _velocitySources.Add(id, new VelocitySourceState {
            velocity = velocity,
            remaining = duration,
        });
    }

    public void ClearVelocitySource(int id) {
        _velocitySources.Remove(id);
    }

    public void ApplyImpulseWithoutSnap(Vector3 impulse) {
        var xz = new Vector3(impulse.x, 0, impulse.z);
        ApplyImpulse(xz);
        Jump(impulse.y);
    }

    public void Jump(float jumpStrength) {
        _velocityY = Mathf.Sqrt(jumpStrength * -2f * _settings.gravity);
        _noSnapTimer = NoSnapDuration;
    }

    private bool ApplyPointForces(float dt) {
        if (_pointForces.Count == 0) return false;

        _pointForcesToRemove.Clear();
        var breaksGroundSnap = false;
        foreach (var pair in _pointForces) {
            var state = pair.Value;
            state.remaining -= dt;
            if (state.remaining <= 0f) {
                _pointForcesToRemove.Add(pair.Key);
                continue;
            }

            var direction = ComputePointForceDirection(state.point, state.vectorMode, state.upBias);
            ApplyForce(direction * state.forcePerSecond, dt);
            if (direction.y > 0.0001f) {
                breaksGroundSnap = true;
            }
        }

        for (var i = 0; i < _pointForcesToRemove.Count; i++)
            _pointForces.Remove(_pointForcesToRemove[i]);

        if (breaksGroundSnap) {
            _noSnapTimer = Mathf.Max(_noSnapTimer, NoSnapDuration);
        }

        return breaksGroundSnap;
    }

    private Vector3 ApplyVelocitySources(float dt, out bool breaksGroundSnap) {
        breaksGroundSnap = false;
        if (_velocitySources.Count == 0)
            return Vector3.zero;

        _velocitySourcesToRemove.Clear();
        var velocity = Vector3.zero;
        foreach (var pair in _velocitySources) {
            var state = pair.Value;
            state.remaining -= dt;
            if (state.remaining <= 0f) {
                _velocitySourcesToRemove.Add(pair.Key);
                continue;
            }

            velocity += state.velocity;
            if (state.velocity.y > 0.0001f)
                breaksGroundSnap = true;
        }

        for (var i = 0; i < _velocitySourcesToRemove.Count; i++)
            _velocitySources.Remove(_velocitySourcesToRemove[i]);

        if (breaksGroundSnap)
            _noSnapTimer = Mathf.Max(_noSnapTimer, NoSnapDuration);

        return velocity;
    }

    private Vector3 ComputePointForceDirection(Vector3 point, SpellKnockbackVectorMode vectorMode, float upBias) {
        return SpellKnockbackDirectionUtility.ComputeDirection(transform, point, vectorMode, upBias);
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
        if ((flags & CollisionFlags.Sides) != 0) {
            _externalVelocity.x = 0f;
            _externalVelocity.z = 0f;
        }

        if ((flags & CollisionFlags.Above) != 0 && _externalVelocity.y > 0f) {
            _externalVelocity.y = 0f;
        }

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