using UnityEngine;

public class DotDamage : IProjectileDamage {
    private readonly SpellData data;
    private readonly SpellProjectile projectile;

    public DotDamage(SpellProjectile p, SpellData d) {
        projectile = p;
        data = d;
    }

    public void OnHit(Collider other) {
    }

    public void OnStay(Collider other) {
        DamageUtils.TryApplyDamage(projectile, data, other);
    }
}