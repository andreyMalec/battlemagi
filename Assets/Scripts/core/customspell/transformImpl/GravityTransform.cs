using UnityEngine;

public class GravityTransform : ISpellTransform {
    public Transform Transform { get; private set; }

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
        Transform = transform;
        _inner.Init(transform, ctx);
    }

    public void Tick(float dt) {
        Motion = new SpellMotion { Velocity = Motion.Velocity + _gravity * dt };
        _inner.Tick(dt);
        var dir = Motion.Velocity;
        if (dir.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        var original = Motion;
        Motion = new SpellMotion { Velocity = original.Velocity + _gravity * dt };
        var sampled = _inner.Sample(dt);
        Motion = original;
        return sampled;
    }

    public void SetForward(Vector3 forward) {
        _inner.SetForward(forward);
    }
}