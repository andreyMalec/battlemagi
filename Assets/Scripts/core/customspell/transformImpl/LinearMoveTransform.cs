using UnityEngine;

public class LinearMoveTransform : ISpellTransform {
    public SpellMotion Motion { get; set; }

    private Transform _transform;

    public LinearMoveTransform(Vector3 dir, float speed) {
        Motion = new SpellMotion { Velocity = dir.normalized * speed };
    }

    public void Init(Transform transform, ISpellContext ctx) {
        _transform = transform;
    }

    public void Tick(float dt) {
        _transform.position += Motion.Velocity * dt;
    }

    public Vector3 Sample(float dt) {
        return _transform.position + Motion.Velocity * dt;
    }
}