using UnityEngine;

public static class SpellEffectResolver {
    public static bool CanAffect(EffectDefinition def, ISpellContext context, GameObject targetGo, ulong targetOwner) {
        if (def == null) return false;
        if (context == null) return false;

        if (IsDraggableTarget(targetGo))
            return def.target.HasFlag(EffectTarget.Draggable);

        if (targetOwner == ulong.MaxValue)
            return false;

        if (targetOwner == context.OwnerId)
            return def.target.HasFlag(EffectTarget.Self);

        if (TeamManager.Instance.AreEnemies(context.OwnerId, targetOwner))
            return def.target.HasFlag(EffectTarget.Enemies);

        return def.target.HasFlag(EffectTarget.Allies);
    }

    public static bool TryGetStatusable(GameObject targetGo, out Statusable statusable, out ulong ownerId) {
        statusable = null;
        ownerId = ulong.MaxValue;
        if (targetGo == null) return false;

        if (DamageUtils.TryGetOwnerFromCollider(targetGo, out _, out ownerId)) {
            return targetGo.TryGetComponent(out statusable) || (statusable = targetGo.GetComponentInParent<Statusable>()) != null;
        }

        if (!targetGo.TryGetComponent(out statusable)) {
            statusable = targetGo.GetComponentInParent<Statusable>();
        }

        if (statusable == null) return false;

        var damageable = statusable.GetComponent<Damageable>();
        if (damageable != null)
            ownerId = damageable.OwnerId;

        return true;
    }

    public static bool IsDraggableTarget(GameObject targetGo) {
        if (targetGo == null)
            return false;

        if (targetGo.TryGetComponent<Draggable>(out _))
            return true;

        return targetGo.GetComponentInParent<Draggable>() != null;
    }
}
