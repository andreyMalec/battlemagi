using UnityEngine;

public class LookAtPointTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }

    private readonly float _speed;
    private readonly float _maxDistance;
    private readonly LayerMask _mask;

    private Transform _transform;
    private ISpellContext _ctx;

    private Vector3 _lastValidTarget;
    private bool _hasLastValid;

    public LookAtPointTransform(float speed, float maxDistance, LayerMask mask) {
        _speed = speed;
        _maxDistance = maxDistance;
        _mask = mask;
        Motion = default;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        _transform = transform;
        _ctx = ctx;
        _hasLastValid = false;
        _lastValidTarget = transform.position;
    }

    public void Tick(float dt) {
        var target = GetTarget();
        var to = target - _transform.position;
        var dir = to.sqrMagnitude > 0f ? to.normalized : Vector3.zero;

        Motion = new SpellMotion { Velocity = dir * _speed };
        _transform.position += Motion.Velocity * dt;
    }

    public Vector3 Sample(float dt) {
        var target = GetTarget();
        var to = target - _transform.position;
        var dir = to.sqrMagnitude > 0f ? to.normalized : Vector3.zero;
        return _transform.position + dir * (_speed * dt);
    }

    private Vector3 GetTarget() {
        var ray = new Ray(_ctx.Caster.spawnPos.transform.position, _ctx.Caster.spawnPos.transform.forward);
        if (Physics.Raycast(ray, out var hit, _maxDistance, _mask, QueryTriggerInteraction.Ignore)) {
            _lastValidTarget = hit.point;
            _hasLastValid = true;
            return hit.point;
        }

        return _hasLastValid
            ? _lastValidTarget
            : (_ctx.Caster.spawnPos.transform.position + _ctx.Caster.spawnPos.transform.forward * _maxDistance);
    }
}