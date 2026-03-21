using UnityEngine;

public class LookAtPointTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }

    private readonly float _speed;
    private readonly float _maxDistance;
    private readonly LayerMask _mask;

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
        Transform = transform;
        _ctx = ctx;
        _hasLastValid = false;
        _lastValidTarget = transform.position;
    }

    public void Tick(float dt) {
        var target = GetTarget();
        var to = target - Transform.position;
        var dir = to.sqrMagnitude > 0f ? to.normalized : Vector3.zero;

        Motion = new SpellMotion { Velocity = dir * _speed };
        Transform.position += Motion.Velocity * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));

        if (dir.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        var target = GetTarget();
        var to = target - Transform.position;
        var dir = to.sqrMagnitude > 0f ? to.normalized : Vector3.zero;
        return Transform.position + dir * (_speed * dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
    }

    private Vector3 GetTarget() {
        var ray = new Ray(_ctx.Caster.Origin, _ctx.Caster.Direction);
        if (Physics.Raycast(ray, out var hit, _maxDistance, _mask, QueryTriggerInteraction.Ignore)) {
            _lastValidTarget = hit.point;
            _hasLastValid = true;
            return hit.point;
        }

        return _hasLastValid
            ? _lastValidTarget
            : (_ctx.Caster.Origin + _ctx.Caster.Direction * _maxDistance);
    }

    public void SetForward(Vector3 forward) {
    }
}