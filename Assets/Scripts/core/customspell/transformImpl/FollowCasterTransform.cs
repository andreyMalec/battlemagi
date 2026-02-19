using UnityEngine;

public enum FollowCasterTarget {
    Caster,
    Spawn
}

public class FollowCasterTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }

    private readonly FollowCasterTarget _target;
    private ISpellContext _ctx;

    public FollowCasterTransform(FollowCasterTarget target) {
        _target = target;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
        Transform = transform;
        _ctx = ctx;
    }

    public void Tick(float dt) {
        Transform.position = Sample(dt);
        var dir = _ctx.Caster.Direction;
        if (dir.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        return _target switch {
            FollowCasterTarget.Caster => _ctx.Caster.transform.position,
            FollowCasterTarget.Spawn => _ctx.Caster.Origin,
            _ => Transform.position
        };
    }
}