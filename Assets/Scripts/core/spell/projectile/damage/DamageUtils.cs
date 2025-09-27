using System.Linq;
using Unity.Netcode;
using UnityEngine;

public static class DamageUtils {
    public static ulong TryApplyDamage(
        SpellProjectile projectile,
        SpellData data,
        Collider other,
        ulong[] excludeClients = null,
        bool applyDistanceMultiplier = false
    ) {
        if (!other.TryGetComponent<Damageable>(out var damageable))
            return ulong.MaxValue;

        var netObj = other.GetComponent<NetworkObject>();
        if (!data.canSelfDamage && projectile.OwnerClientId == netObj.OwnerClientId)
            return ulong.MaxValue;

        if (excludeClients != null && excludeClients.Contains(netObj.OwnerClientId))
            return ulong.MaxValue;

        if (applyDistanceMultiplier) {
            var distance = Vector3.Distance(projectile.transform.position, other.transform.position);
            var damageMultiplier = 1f - distance / data.areaRadius;
            damageable.TakeDamage(projectile.OwnerClientId, data.baseDamage * damageMultiplier, data.damageSound);
        } else {
            damageable.TakeDamage(projectile.OwnerClientId, data.baseDamage, data.damageSound);
        }

        return netObj.OwnerClientId;
    }
}