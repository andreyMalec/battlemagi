using UnityEngine;

public class FollowCasterTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }

    private Transform _transform;
    private ISpellContext _ctx;

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
        _transform = transform;
        _ctx = ctx;
    }

    public void Tick(float dt) {
        _transform.position = _ctx.Caster.transform.position;
    }

    public Vector3 Sample(float dt) {
        return _ctx.Caster.transform.position;
    }
}