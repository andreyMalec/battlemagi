using UnityEngine;

public class StaticTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }
    private ISpellContext _ctx;

    public void Init(Transform transform, ISpellContext ctx) {
        Motion = default;
        _ctx = ctx;
        Transform = transform;
    }

    public void Tick(float dt) {
    }

    public Vector3 Sample(float dt) {
        return Transform.position;
    }

    public void SetForward(Vector3 forward) {
    }
}