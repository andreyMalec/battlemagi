using System.Linq;
using UnityEngine;

public static class DamageUtils {
    public static bool TryGetOwnerFromCollider(Collider other, out Damageable damageable, out ParticipantId owner) {
        damageable = null;
        owner = default;

        if (other.TryGetComponent<ChildCollider>(out _)) {
            damageable = other.GetComponentInParent<Damageable>();
        } else if (!other.TryGetComponent(out damageable)) {
            return false;
        }

        if (damageable == null) return false;

        owner = damageable.OwnerId;

        return true;
    }

    public static bool TryGetOwnerFromCollider(GameObject other, out Damageable damageable, out ParticipantId owner) {
        damageable = null;
        owner = default;

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