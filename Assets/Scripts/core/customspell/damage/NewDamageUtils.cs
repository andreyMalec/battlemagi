using System.Linq;
using UnityEngine;

public static class NewDamageUtils {
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