using UnityEngine;

[System.Flags]
public enum StepDistanceAxes {
    None = 0,
    X = 1 << 0,
    Y = 1 << 1,
    Z = 1 << 2,
    All = X | Y | Z
}

public class StepDistanceTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    private readonly ISpellTransform _inner;
    private readonly float _stepDistance;
    private readonly StepDistanceAxes _axes;

    private ISpellContext _ctx;

    private Vector3 _prevPosition;
    private float _distance;
    private float _nextStep;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    public StepDistanceTransform(ISpellTransform inner, float stepDistance, StepDistanceAxes axes) {
        _inner = inner;
        _stepDistance = stepDistance;
        _axes = axes;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
        _inner.Init(transform, ctx);

        _prevPosition = transform.position;
        _distance = 0f;
        _nextStep = _stepDistance;
    }

    public void Tick(float dt) {
        _inner.Tick(dt);

        var pos = Transform.position;
        var delta = pos - _prevPosition;
        if ((_axes & StepDistanceAxes.X) == 0) delta.x = 0f;
        if ((_axes & StepDistanceAxes.Y) == 0) delta.y = 0f;
        if ((_axes & StepDistanceAxes.Z) == 0) delta.z = 0f;
        _distance += delta.magnitude;
        _prevPosition = pos;

        if (_stepDistance <= 0f) return;

        var sent = false;
        while (_distance >= _nextStep) {
            if (!sent)
                _ctx.SendEvent(new OnStepDistanceEvent {
                    stepDistance = _stepDistance,
                    totalDistance = _nextStep,
                    point = pos,
                    forward = Motion.Velocity.normalized
                });
            sent = true;
            _nextStep += _stepDistance;
        }
    }

    public Vector3 Sample(float dt) {
        return _inner.Sample(dt);
    }

    public void SetForward(Vector3 forward) {
        _inner.SetForward(forward);
    }
}