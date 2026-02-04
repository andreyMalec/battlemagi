using UnityEngine;

public class SpiralMoveTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }

    private readonly Vector3 _forward;
    private readonly SpiralAxis _axisMode;

    private readonly float _angularSpeed;
    private readonly float _radius;

    private Transform _transform;
    private Vector3 _center;

    private float _angle;

    public SpiralMoveTransform(Vector3 forward, SpiralAxis axisMode, float radius, float angularSpeed, float forwardSpeed) {
        _forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
        _axisMode = axisMode;
        _radius = radius;
        _angularSpeed = angularSpeed;
        _angle = 0f;

        Motion = new SpellMotion { Velocity = _forward * forwardSpeed };
    }

    public void Init(Transform transform, ISpellContext ctx) {
        _transform = transform;
        _center = transform.position;
        _transform.position = _center;
    }

    public void Tick(float dt) {
        _center += Motion.Velocity * dt;
        _angle += _angularSpeed * dt;
        _transform.position = _center + CalcRadial(_angle) * _radius;
    }

    public Vector3 Sample(float dt) {
        var nextCenter = _center + Motion.Velocity * dt;
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
            SpiralAxis.LocalX => _transform.right,
            SpiralAxis.LocalY => _transform.up,
            SpiralAxis.LocalZ => _transform.forward,
            _ => _forward
        };
    }
}
