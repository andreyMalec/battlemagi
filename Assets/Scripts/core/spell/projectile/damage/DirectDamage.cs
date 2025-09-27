using UnityEngine;

public class DirectDamage : IProjectileDamage {
    private readonly SpellData data;
    private readonly SpellProjectile projectile;

    public DirectDamage(SpellProjectile p, SpellData d) {
        projectile = p;
        data = d;
    }

    public void OnHit(Collider other) {
        DamageUtils.TryApplyDamage(projectile, data, other);
    }

    public void OnStay(Collider other) {
    }
}