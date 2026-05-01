using UnityEngine;

public class MaxDistanceTransform : ISpellTransform {
    private const float ReturnArrivalDistanceSqr = 0.75f;

    public Transform Transform { get; private set; }

    private readonly ISpellTransform _inner;
    private readonly float _maxDistance;
    private readonly bool _returnToCaster;

    private ISpellContext _ctx;

    private Vector3 _prevPosition;
    private float _distance;
    private bool _reached;
    private bool _returning;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    public MaxDistanceTransform(ISpellTransform inner, float maxDistance, bool returnToCaster = false) {
        _inner = inner;
        _maxDistance = maxDistance;
        _returnToCaster = returnToCaster;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
        _inner.Init(transform, ctx);

        _prevPosition = transform.position;
        _distance = 0f;
        _reached = false;
        _returning = false;
    }

    public void Tick(float dt) {
        if (_reached) return;

        if (_returning) {
            TickReturn(dt);
            return;
        }

        _inner.Tick(dt);

        var pos = Transform.position;
        _distance += Vector3.Distance(_prevPosition, pos);
        _prevPosition = pos;

        if (_distance < _maxDistance) return;

        _ctx.SendEvent(new OnMaxDistanceEvent {
            maxDistance = _maxDistance,
            point = pos,
            forward = Motion.Velocity.normalized
        });

        if (_returnToCaster) {
            _returning = true;
            _ctx.Event.OnReturnToCaster(_ctx);
            return;
        }

        _reached = true;
        Motion = new SpellMotion { Velocity = Vector3.zero };
    }

    public Vector3 Sample(float dt) {
        if (_reached) return Transform.position;

        if (_returning) {
            var toCaster = _ctx.Caster.Origin - Transform.position;
            if (toCaster.sqrMagnitude <= ReturnArrivalDistanceSqr)
                return _ctx.Caster.Origin;

            _inner.SetForward(toCaster);
        }

        return _inner.Sample(dt);
    }

    public void SetForward(Vector3 forward) {
        if (_returning) return;
        _inner.SetForward(forward);
    }

    private void TickReturn(float dt) {
        var toCaster = _ctx.Caster.Origin - Transform.position;
        if (toCaster.sqrMagnitude <= ReturnArrivalDistanceSqr) {
            CompleteReturn();
            return;
        }

        _inner.SetForward(toCaster);
        _inner.Tick(dt);
        _prevPosition = Transform.position;

        if ((_ctx.Caster.Origin - Transform.position).sqrMagnitude <= ReturnArrivalDistanceSqr)
            CompleteReturn();
    }

    private void CompleteReturn() {
        Transform.position = _ctx.Caster.Origin;
        _returning = false;
        _reached = true;
        Motion = new SpellMotion { Velocity = Vector3.zero };

        _ctx.SendEvent(new OnLifetimeEndingEvent { remaining = 0 });
        _ctx.View.Kill(_ctx);
    }
}