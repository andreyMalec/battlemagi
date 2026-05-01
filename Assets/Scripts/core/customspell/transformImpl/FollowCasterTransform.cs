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
    private int _index;
    private int _count;

    public FollowCasterTransform(FollowCasterTarget target) {
        _target = target;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
        Transform = transform;
        _ctx = ctx;
        if (transform.TryGetComponent<ArcIndex>(out var index)) {
            _index = index.Index;
            _count = index.Count;
        }
    }

    public void Tick(float dt) {
        Transform.position = Sample(dt);
        var dir = _ctx.Caster.Direction;
        if (dir.sqrMagnitude > 0f) {
            if (_count > 0) {
                var angleStep = _ctx.Spell.spawn.arcAngleStep;
                var startAngle = -((_count - 1) * angleStep) / 2f;
                var angle = startAngle + angleStep * _index;
                Transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, angle, 0f);
            } else {
                Transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }

    public Vector3 Sample(float dt) {
        return _target switch {
            FollowCasterTarget.Caster => _ctx.Caster.transform.position,
            FollowCasterTarget.Spawn => _ctx.Caster.Origin,
            _ => Transform.position
        };
    }

    public void SetForward(Vector3 forward) {
    }
}