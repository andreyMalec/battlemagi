using Unity.Netcode;

public static class DamageRelationship {
    public static bool CanDamage(ISpellContext context, Damageable target, ulong targetOwner) {
        if (context == null) return false;
        if (target == null) return false;

        var def = context.SpellDamage;
        if (def == null) return true;
        if (def.canHitAllies) return true;
        if (target.IsStructure && target.TryGetComponent<NetworkObject>(out var networkObject) &&
            networkObject.IsSceneObject == true) return true;

        return TeamManager.Instance.AreEnemies(context.OwnerId, targetOwner);
    }
}