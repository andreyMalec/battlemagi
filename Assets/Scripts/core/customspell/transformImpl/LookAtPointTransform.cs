using UnityEngine;

public class LookAtPointTransform : ISpellTransform {
    private const float MinTargetDistanceSqr = 0.0001f;

    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }

    private readonly float _speed;
    private readonly float _maxDistance;
    private readonly LayerMask _mask;

    private ISpellContext _ctx;

    private Vector3 _lastMoveDirection;
    private bool _arrived;

    public LookAtPointTransform(float speed, float maxDistance, LayerMask mask) {
        _speed = speed;
        _maxDistance = maxDistance;
        _mask = mask;
        Motion = default;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
        _lastMoveDirection = transform.forward;
        _arrived = false;
        GetTarget();
    }

    public void Tick(float dt) {
        if (_arrived) {
            Motion = default;
            return;
        }

        var target = GetTarget();
        var to = target - Transform.position;
        var distance = to.magnitude;
        if (distance * distance <= MinTargetDistanceSqr) {
            Transform.position = target;
            Motion = default;
            _arrived = true;
            _ctx.SendEvent(new OnLifetimeEndingEvent { remaining = 0 });
            _ctx.View.Kill(_ctx);
            return;
        }

        var dir = to / distance;
        var step = _speed * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
        var moveDistance = Mathf.Min(step, distance);

        Motion = new SpellMotion { Velocity = dir * _speed };
        Transform.position += dir * moveDistance;
        _lastMoveDirection = dir;

        if (moveDistance >= distance - 0.0001f) {
            Transform.position = target;
            Motion = default;
            _arrived = true;
            _ctx.SendEvent(new OnLifetimeEndingEvent { remaining = 0 });
            _ctx.View.Kill(_ctx);
        }

        if (_lastMoveDirection.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(_lastMoveDirection, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        if (_arrived)
            return Transform.position;

        var target = GetTarget();
        var to = target - Transform.position;
        var distance = to.magnitude;
        if (distance * distance <= MinTargetDistanceSqr)
            return target;

        var dir = to / distance;
        var step = _speed * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
        return Transform.position + dir * Mathf.Min(step, distance);
    }

    private Vector3 GetTarget() {
        var ray = new Ray(_ctx.Caster.Origin, _ctx.Caster.Direction);
        if (Physics.Raycast(ray, out var hit, _maxDistance, _mask, QueryTriggerInteraction.Ignore)) {
            return hit.point;
        }

        return _ctx.Caster.Origin + _ctx.Caster.Direction * _maxDistance;
    }

    public void SetForward(Vector3 forward) {
    }
}