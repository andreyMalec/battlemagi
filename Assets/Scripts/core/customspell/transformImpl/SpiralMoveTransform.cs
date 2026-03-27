using UnityEngine;

public class SpiralMoveTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }

    private Vector3 _forward;
    private readonly SpiralAxis _axisMode;

    private readonly float _angularSpeed;
    private readonly float _radius;

    private Vector3 _center;

    private float _angle;
    private ISpellContext _ctx;

    public SpiralMoveTransform(
        Vector3 forward,
        SpiralAxis axisMode,
        float radius,
        float angularSpeed,
        float forwardSpeed
    ) {
        _forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
        _axisMode = axisMode;
        _radius = radius;
        _angularSpeed = angularSpeed;
        _angle = 0f;

        Motion = new SpellMotion { Velocity = _forward * forwardSpeed };
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _center = transform.position;
        Transform.position = _center;
        _ctx = ctx;
    }

    public void Tick(float dt) {
        var prev = Transform.position;

        _center += Motion.Velocity * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
        _angle += _angularSpeed * dt;
        Transform.position = _center + CalcRadial(_angle) * _radius;

        var vel = (Transform.position - prev) / dt;
        if (vel.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        var nextCenter = _center + Motion.Velocity * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
        var nextAngle = _angle + _angularSpeed * dt;
        return nextCenter + CalcRadial(nextAngle) * _radius;
    }

    private Vector3 CalcRadial(float angle) {
        var axis = GetAxis();

        var refUp = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        var right = Vector3.Cross(refUp, axis);
        if (right.sqrMagnitude > 0f) right.Normalize();
        var up = Vector3.Cross(axis, right);
        if (up.sqrMagnitude > 0f) up.Normalize();

        return right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
    }

    private Vector3 GetAxis() {
        return _axisMode switch {
            SpiralAxis.Forward => _forward,
            SpiralAxis.WorldX => Vector3.right,
            SpiralAxis.WorldY => Vector3.up,
            SpiralAxis.WorldZ => Vector3.forward,
            SpiralAxis.LocalX => Transform.right,
            SpiralAxis.LocalY => Transform.up,
            SpiralAxis.LocalZ => Transform.forward,
            _ => _forward
        };
    }

    public void SetForward(Vector3 forward) {
        var dir = forward.sqrMagnitude > 0f ? forward.normalized : Transform.forward;

        var speed = Motion.Velocity.magnitude;
        Motion = new SpellMotion { Velocity = dir * speed };

        if (_axisMode == SpiralAxis.Forward)
            _forward = dir;
    }
}