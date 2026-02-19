using UnityEngine;

public class MaxDistanceTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    private readonly ISpellTransform _inner;
    private readonly float _maxDistance;

    private ISpellContext _ctx;

    private Vector3 _prevPosition;
    private float _distance;
    private bool _reached;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    public MaxDistanceTransform(ISpellTransform inner, float maxDistance) {
        _inner = inner;
        _maxDistance = maxDistance;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
        _inner.Init(transform, ctx);

        _prevPosition = transform.position;
        _distance = 0f;
        _reached = false;
    }

    public void Tick(float dt) {
        if (_reached) return;

        _inner.Tick(dt);

        var pos = Transform.position;
        _distance += Vector3.Distance(_prevPosition, pos);
        _prevPosition = pos;

        if (_distance < _maxDistance) return;

        _reached = true;
        _ctx.SendEvent(new OnMaxDistanceEvent {
            maxDistance = _maxDistance,
            point = pos,
            forward = Motion.Velocity.normalized
        });
        Motion = new SpellMotion { Velocity = Vector3.zero };
    }

    public Vector3 Sample(float dt) {
        if (_reached) return Transform.position;
        return _inner.Sample(dt);
    }
}