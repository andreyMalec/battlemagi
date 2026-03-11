using UnityEngine;

public class LinearMoveTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion { get; set; }
    
    private ISpellContext _ctx;

    public LinearMoveTransform(Vector3 dir, float speed) {
        Motion = new SpellMotion { Velocity = dir.normalized * speed };
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
    }

    public void Tick(float dt) {
        Transform.position += Motion.Velocity * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
    }

    public Vector3 Sample(float dt) {
        return Transform.position + Motion.Velocity * (dt * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));
    }
}