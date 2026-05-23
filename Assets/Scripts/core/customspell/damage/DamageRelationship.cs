using Unity.Netcode;
using UnityEngine;

public static class DamageRelationship {
    public static bool CanDamage(ISpellContext context, Damageable target, ParticipantId targetOwner) {
        if (context == null) return false;
        if (target == null) return false;

        var def = context.SpellDamage;
        if (def == null) return true;
        if (def.canHitAllies) return true;
        if (target.IsStructure && target.TryGetComponent<NetworkObject>(out var networkObject) &&
            networkObject.IsSceneObject == true) return true;

        return AreEnemies(context, targetOwner);
    }

    public static bool AreEnemies(ISpellContext context, ParticipantId targetOwner) {
        var attackerId = context.OwnerId;

        if (TeamManager.Instance == null)
            return attackerId != targetOwner;

        return TeamManager.Instance.AreEnemies(attackerId, targetOwner);
    }

    public static bool AreAllies(ISpellContext context, ParticipantId targetOwner) {
        return !AreEnemies(context, targetOwner);
    }

    public static bool IsSelf(ISpellContext context, ParticipantId targetOwner) {
        var attackerId = context.OwnerId;

        return attackerId == targetOwner;
    }

    public static bool TryGetTargetParticipant(
        GameObject targetObject, out ParticipantId participantId
    ) {
        participantId = default;
        if (targetObject != null) {
            if (targetObject.TryGetComponent<ParticipantIdentity>(out var identity) ||
                (identity = targetObject.GetComponentInParent<ParticipantIdentity>()) != null) {
                participantId = identity.Id;
                return true;
            }
        }

        participantId = default;
        return true;
    }
}