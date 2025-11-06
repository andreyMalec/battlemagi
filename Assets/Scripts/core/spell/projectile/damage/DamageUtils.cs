using System.Linq;
using Unity.Netcode;
using UnityEngine;

public static class DamageUtils {
    public static ulong TryApplyDamage(
        BaseSpell spell,
        SpellData data,
        Damageable damageable,
        Collider other,
        ulong[] excludeClients = null,
        bool applyDistanceMultiplier = false
    ) {
        var netObj = damageable.GetComponent<NetworkObject>();
        if (!data.canSelfDamage && TeamManager.Instance.AreAllies(spell.OwnerClientId, netObj.OwnerClientId))
            return ulong.MaxValue;

        if (excludeClients != null && excludeClients.Contains(netObj.OwnerClientId))
            return ulong.MaxValue;

        var damageMulti = spell.damageMultiplier;
        if (applyDistanceMultiplier) {
            var distance = Vector3.Distance(spell.transform.position, other.transform.position);
            var areaDamageMulti = 1f - distance / data.areaRadius;
            damageable.TakeDamage(spell.OwnerClientId, data.baseDamage * areaDamageMulti * damageMulti,
                data.damageSound);
        } else {
            damageable.TakeDamage(spell.OwnerClientId, data.baseDamage * damageMulti, data.damageSound);
        }

        return netObj.OwnerClientId;
    }

    public static ulong TryApplyDamage(
        BaseSpell spell,
        SpellData data,
        Collider other,
        ulong[] excludeClients = null,
        bool applyDistanceMultiplier = false
    ) {
        if (!TryGetOwnerFromCollider(other, out var damageable, out var owner))
            return ulong.MaxValue;

        return TryApplyDamage(spell, data, damageable, other, excludeClients, applyDistanceMultiplier);
    }

    // Helper: resolve Damageable and owner client id from a collider without applying damage.
    // Returns true if a Damageable and NetworkObject owner were found; out parameters are set accordingly.
    public static bool TryGetOwnerFromCollider(Collider other, out Damageable damageable, out ulong owner) {
        damageable = null;
        owner = ulong.MaxValue;

        if (other.TryGetComponent<ChildCollider>(out _)) {
            damageable = other.GetComponentInParent<Damageable>();
        } else if (!other.TryGetComponent(out damageable)) {
            return false;
        }

        var netObj = damageable.GetComponent<NetworkObject>();
        if (netObj == null) return false;

        owner = netObj.OwnerClientId;
        return true;
    }
}