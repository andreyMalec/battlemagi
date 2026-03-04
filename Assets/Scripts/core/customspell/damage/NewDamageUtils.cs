using System.Linq;
using UnityEngine;

public static class NewDamageUtils {
    public static ulong TryApplyDamage(
        BaseSpell spell,
        SpellData data,
        Collider other,
        ulong[] excludeClients = null,
        bool applyDistanceMultiplier = false
    ) {
        if (!TryGetOwnerFromCollider(other, out var damageable, out var owner))
            return ulong.MaxValue;

        if (!data.canSelfDamage && TeamManager.Instance.AreAllies(spell.OwnerClientId, owner))
            return ulong.MaxValue;

        if (excludeClients != null && excludeClients.Contains(owner))
            return ulong.MaxValue;

        var damageMulti = spell.damageMultiplier;
        if (applyDistanceMultiplier) {
            var distance = Vector3.Distance(spell.transform.position, other.transform.position);
            var areaDamageMulti = 1f - distance / data.areaRadius;
            damageMulti *= areaDamageMulti;
        }

        var amount = data.baseDamage * damageMulti;
        damageable.TakeDamage(data.name, spell.OwnerClientId, amount, data.damageSound);
        return owner;
    }

    public static bool TryGetOwnerFromCollider(Collider other, out Damageable damageable, out ulong owner) {
        damageable = null;
        owner = ulong.MaxValue;

        if (other.TryGetComponent<ChildCollider>(out _)) {
            damageable = other.GetComponentInParent<Damageable>();
        } else if (!other.TryGetComponent(out damageable)) {
            return false;
        }

        if (damageable == null) return false;

        owner = damageable.OwnerId;

        return true;
    }

    public static bool TryGetOwnerFromCollider(GameObject other, out Damageable damageable, out ulong owner) {
        damageable = null;
        owner = ulong.MaxValue;

        if (other.TryGetComponent<ChildCollider>(out _)) {
            damageable = other.GetComponentInParent<Damageable>();
        } else if (!other.TryGetComponent(out damageable)) {
            return false;
        }

        if (damageable == null) return false;

        owner = damageable.OwnerId;

        return true;
    }
}