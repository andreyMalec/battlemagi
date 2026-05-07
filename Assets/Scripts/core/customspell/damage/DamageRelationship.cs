using Unity.Netcode;
using UnityEngine;

public static class DamageRelationship {
    public static bool CanDamage(ISpellContext context, Damageable target, ulong targetOwner) {
        if (context == null) return false;
        if (target == null) return false;

        var def = context.SpellDamage;
        if (def == null) return true;
        if (def.canHitAllies) return true;
        if (target.IsStructure && target.TryGetComponent<NetworkObject>(out var networkObject) &&
            networkObject.IsSceneObject == true) return true;

        return AreEnemies(context, targetOwner, target.gameObject);
    }

    public static bool AreEnemies(ISpellContext context, ulong targetOwner, GameObject targetObject = null) {
        var attackerId = TryGetCasterParticipant(context, out var attackerParticipant)
            ? attackerParticipant
            : ParticipantOwnerCodec.Decode(context.OwnerId);

        var targetId = TryGetTargetParticipant(targetObject, targetOwner, out var targetParticipant)
            ? targetParticipant
            : ParticipantOwnerCodec.Decode(targetOwner);

        if (TeamManager.Instance == null)
            return attackerId != targetId;

        return TeamManager.Instance.AreEnemies(attackerId, targetId);
    }

    public static bool AreAllies(ISpellContext context, ulong targetOwner, GameObject targetObject = null) {
        return !AreEnemies(context, targetOwner, targetObject);
    }

    public static bool IsSelf(ISpellContext context, ulong targetOwner, GameObject targetObject = null) {
        var attackerId = TryGetCasterParticipant(context, out var attackerParticipant)
            ? attackerParticipant
            : ParticipantOwnerCodec.Decode(context.OwnerId);

        var targetId = TryGetTargetParticipant(targetObject, targetOwner, out var targetParticipant)
            ? targetParticipant
            : ParticipantOwnerCodec.Decode(targetOwner);

        return attackerId == targetId;
    }

    private static bool TryGetCasterParticipant(ISpellContext context, out ParticipantId participantId) {
        participantId = default;
        if (context?.Caster == null)
            return false;
        if (!context.Caster.TryGetComponent<ParticipantIdentity>(out var identity))
            return false;

        participantId = identity.Id;
        return true;
    }

    private static bool TryGetTargetParticipant(GameObject targetObject, ulong fallbackOwnerId, out ParticipantId participantId) {
        participantId = default;
        if (targetObject != null) {
            if (targetObject.TryGetComponent<ParticipantIdentity>(out var identity) ||
                (identity = targetObject.GetComponentInParent<ParticipantIdentity>()) != null) {
                participantId = identity.Id;
                return true;
            }
        }

        if (fallbackOwnerId == ulong.MaxValue)
            return false;

        participantId = ParticipantOwnerCodec.Decode(fallbackOwnerId);
        return true;
    }
}