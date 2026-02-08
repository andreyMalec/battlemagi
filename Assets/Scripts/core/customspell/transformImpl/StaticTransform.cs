using UnityEngine;

public class StaticTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }
    private ISpellContext _ctx;

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
        _ctx = ctx;
    }

    public void Tick(float dt) {
    }

    public Vector3 Sample(float dt) {
        return _ctx.View.transform.position;
    }
}