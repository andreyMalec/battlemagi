using UnityEngine;

public class GravityTransform : ISpellTransform {
    private readonly ISpellTransform _inner;
    private readonly Vector3 _gravity;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    public GravityTransform(ISpellTransform inner, Vector3 gravity) {
        _inner = inner;
        _gravity = gravity;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        _inner.Init(transform, ctx);
    }

    public void Tick(float dt) {
        Motion = new SpellMotion { Velocity = Motion.Velocity + _gravity * dt };
        _inner.Tick(dt);
    }

    public Vector3 Sample(float dt) {
        var original = Motion;
        Motion = new SpellMotion { Velocity = original.Velocity + _gravity * dt };
        var sampled = _inner.Sample(dt);
        Motion = original;
        return sampled;
    }
}

