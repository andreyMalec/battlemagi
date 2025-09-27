using UnityEngine;

public class StraightMovement : IProjectileMovement {
    private readonly SpellData data;
    private readonly SpellProjectile projectile;
    private readonly Rigidbody rb;

    public StraightMovement(SpellProjectile p, Rigidbody rb, SpellData data) {
        projectile = p;
        this.rb = rb;
        this.data = data;
    }

    public void Initialize() {
        rb.linearVelocity = projectile.transform.forward * data.baseSpeed;
    }

    public void Tick() {
        // прямое движение — ничего не делаем
    }
}