using System.Linq;
using Unity.Netcode;
using UnityEngine;

public static class DamageUtils {
    public static ulong TryApplyDamage(
        BaseSpell spell,
        SpellData data,
        Collider other,
        ulong[] excludeClients = null,
        bool applyDistanceMultiplier = false
    ) {
        if (!other.TryGetComponent<Damageable>(out var damageable))
            return ulong.MaxValue;

        var netObj = other.GetComponent<NetworkObject>();
        if (!data.canSelfDamage && spell.OwnerClientId == netObj.OwnerClientId)
            return ulong.MaxValue;

        if (excludeClients != null && excludeClients.Contains(netObj.OwnerClientId))
            return ulong.MaxValue;

        if (applyDistanceMultiplier) {
            var distance = Vector3.Distance(spell.transform.position, other.transform.position);
            var damageMultiplier = 1f - distance / data.areaRadius;
            damageable.TakeDamage(spell.OwnerClientId, data.baseDamage * damageMultiplier, data.damageSound);
        } else {
            damageable.TakeDamage(spell.OwnerClientId, data.baseDamage, data.damageSound);
        }

        return netObj.OwnerClientId;
    }
}