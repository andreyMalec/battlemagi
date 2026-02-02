using UnityEngine;

public class LinearMoveTransform : ISpellTransform {
    private readonly Vector3 _velocity;

    public LinearMoveTransform(Vector3 dir, float speed) {
        _velocity = dir.normalized * speed;
    }

    public void Tick(Transform transform, ISpellContext ctx, float dt) {
        transform.position += _velocity * dt;
    }
}