using UnityEngine;

public class LinearMoveTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }

    public LinearMoveTransform(Vector3 dir, float speed) {
        Motion = new SpellMotion { Velocity = dir.normalized * speed };
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
    }

    public void Tick(float dt) {
        Transform.position += Motion.Velocity * dt;
    }

    public Vector3 Sample(float dt) {
        return Transform.position + Motion.Velocity * dt;
    }
}