using UnityEngine;

public class SquashStretchTransform : ISpellTransform {
    private readonly ISpellTransform _inner;
    private readonly float _amplitude;
    private readonly float _frequency;
    private readonly float _damping;

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    private Transform _transform;
    private float _baseScale;
    private float _t;

    public SquashStretchTransform(ISpellTransform inner, float amplitude, float frequency, float damping) {
        _inner = inner;
        _amplitude = amplitude;
        _frequency = frequency;
        _damping = damping;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        _transform = transform;
        _baseScale = transform.localScale.x;
        _t = 0f;
        _inner.Init(transform, ctx);
        ApplyScale(0f);
    }

    public void Tick(float dt) {
        _t += dt;
        _inner.Tick(dt);
        ApplyScale(0f);
    }

    public Vector3 Sample(float dt) {
        return _inner.Sample(dt);
    }

    private void ApplyScale(float extraTime) {
        var time = _t + extraTime;
        var k = _damping <= 0f ? 1f : Mathf.Exp(-_damping * time);
        var wave = Mathf.Sin(time * Mathf.PI * 2f * _frequency);
        var s = Mathf.Max(0f, _baseScale * (1f + _amplitude * k * wave));
        _transform.localScale = Vector3.one * s;
    }
}

