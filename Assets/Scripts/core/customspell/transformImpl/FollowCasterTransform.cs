using UnityEngine;

public enum FollowCasterTarget {
    Caster,
    Spawn
}

public class FollowCasterTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }

    private readonly FollowCasterTarget _target;
    private Transform _transform;
    private ISpellContext _ctx;

    public FollowCasterTransform(FollowCasterTarget target) {
        _target = target;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
        _transform = transform;
        _ctx = ctx;
    }

    public void Tick(float dt) {
        _transform.position = Sample(dt);
        var dir = _ctx.Caster.Direction;
        if (dir.sqrMagnitude > 0f)
            _transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        return _target switch {
            FollowCasterTarget.Caster => _ctx.Caster.transform.position,
            FollowCasterTarget.Spawn => _ctx.Caster.Origin,
            _ => _transform.position
        };
    }
}