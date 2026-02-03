using UnityEngine;

public class BounceTransform : ISpellTransform {
    private readonly ISpellTransform _inner;
    private readonly int _maxBounces;
    private readonly float _speedMultiplier;

    private int _bounces;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    public BounceTransform(ISpellTransform inner, int maxBounces, float speedMultiplier) {
        _inner = inner;
        _maxBounces = maxBounces;
        _speedMultiplier = speedMultiplier;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        _inner.Init(transform, ctx);
    }

    public void Tick(float dt) {
        _inner.Tick(dt);
    }

    public Vector3 Sample(float dt) {
        return _inner.Sample(dt);
    }

    public bool TryBounce(Vector3 normal) {
        if (_bounces >= _maxBounces) return false;
        var v = Motion.Velocity;
        if (v == Vector3.zero) return false;

        var reflected = Vector3.Reflect(v, normal.normalized) * _speedMultiplier;
        Motion = new SpellMotion { Velocity = reflected };
        _bounces++;
        return true;
    }
}