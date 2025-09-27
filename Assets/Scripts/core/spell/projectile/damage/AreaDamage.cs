using UnityEngine;

public class AreaDamage : IProjectileDamage {
    private readonly SpellData data;
    private readonly SpellProjectile projectile;

    public AreaDamage(SpellProjectile p, SpellData d) {
        projectile = p;
        data = d;
    }

    public void OnHit(Collider other) {
        ulong[] exclude = { ulong.MaxValue, ulong.MaxValue };
        exclude[0] = data.canSelfDamage ? ulong.MaxValue : projectile.OwnerClientId;
        exclude[1] = DamageUtils.TryApplyDamage(projectile, data, other);

        var hits = Physics.OverlapSphere(projectile.transform.position, data.areaRadius);
        foreach (var hit in hits) {
            DamageUtils.TryApplyDamage(projectile, data, hit, exclude, true);
        }
    }

    public void OnStay(Collider other) {
    }
}