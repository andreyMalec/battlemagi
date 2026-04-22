using UnityEngine;

public class LinearMoveTransform : ISpellTransform {
    private const float GroundProbePadding = 1f;
    private const float GroundProbeMinDistance = 2f;
    private const float MinDirectionSqrMagnitude = 0.0001f;

    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }

    private readonly bool _moveAlongGround;
    private readonly float _groundOffset;

    private ISpellContext _ctx;

    public LinearMoveTransform(Vector3 dir, float speed, bool moveAlongGround = false, float groundOffset = 0.1f) {
        var direction = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
        Motion = new SpellMotion { Velocity = direction * speed };
        _moveAlongGround = moveAlongGround;
        _groundOffset = groundOffset;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
    }

    public void Tick(float dt) {
        Transform.position = EvaluatePosition(dt, Motion.Velocity, out var velocity);
        Motion = new SpellMotion { Velocity = velocity };
        var dir = Motion.Velocity;
        if (dir.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        return EvaluatePosition(dt, Motion.Velocity, out _);
    }

    public void SetForward(Vector3 forward) {
        var speed = Motion.Velocity.magnitude;
        var dir = forward.sqrMagnitude > 0f ? forward.normalized : Transform.forward;
        if (_moveAlongGround)
            dir = ResolveSurfaceDirection(dir, Transform.position);
        Motion = new SpellMotion { Velocity = dir * speed };
    }

    private Vector3 EvaluatePosition(float dt, Vector3 velocity, out Vector3 resolvedVelocity) {
        var speed = velocity.magnitude;
        if (dt <= 0f || speed <= 0f) {
            resolvedVelocity = velocity;
            return Transform.position;
        }

        if (!_moveAlongGround) {
            resolvedVelocity = velocity;
            return Transform.position + velocity * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
        }

        var direction = ResolveSurfaceDirection(velocity.normalized, Transform.position);
        var distance = speed * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
        var nextPosition = Transform.position + direction * distance;

        if (TryGetGroundHit(nextPosition, distance, out var hit)) {
            direction = ProjectDirectionOnNormal(direction, hit.normal);
            nextPosition = hit.point + hit.normal * _groundOffset;
        }

        resolvedVelocity = direction * speed;
        return nextPosition;
    }

    private Vector3 ResolveSurfaceDirection(Vector3 direction, Vector3 position) {
        if (!TryGetGroundHit(position, 0f, out var hit))
            return direction;

        return ProjectDirectionOnNormal(direction, hit.normal);
    }

    private Vector3 ProjectDirectionOnNormal(Vector3 direction, Vector3 normal) {
        var projected = Vector3.ProjectOnPlane(direction, normal);
        if (projected.sqrMagnitude > MinDirectionSqrMagnitude)
            return projected.normalized;

        if (Transform != null) {
            projected = Vector3.ProjectOnPlane(Transform.forward, normal);
            if (projected.sqrMagnitude > MinDirectionSqrMagnitude)
                return projected.normalized;
        }

        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
    }

    private bool TryGetGroundHit(Vector3 position, float travelDistance, out RaycastHit hit) {
        var probeDistance = Mathf.Max(GroundProbeMinDistance, travelDistance + _groundOffset + GroundProbePadding);
        var origin = position + Vector3.up * probeDistance;
        return Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            probeDistance * 2f,
            _ctx.Spell.defaultRaycast,
            QueryTriggerInteraction.Ignore
        );
    }
}