using UnityEngine;

public class StepDistanceTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    private readonly ISpellTransform _inner;
    private readonly float _stepDistance;

    private ISpellContext _ctx;

    private Vector3 _prevPosition;
    private float _distance;
    private float _nextStep;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    public StepDistanceTransform(ISpellTransform inner, float stepDistance) {
        _inner = inner;
        _stepDistance = stepDistance;
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
        _distance += Vector3.Distance(_prevPosition, pos);
        _prevPosition = pos;

        if (_stepDistance <= 0f) return;

        while (_distance >= _nextStep) {
            _ctx.SendEvent(new OnStepDistanceEvent {
                stepDistance = _stepDistance,
                totalDistance = _nextStep,
                point = pos,
                forward = Motion.Velocity.normalized
            });
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