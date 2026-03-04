public static class DamageRelationship {
    public static bool CanDamage(ISpellContext context, Damageable target, ulong targetOwner) {
        if (context == null) return false;
        if (target == null) return false;

        var def = context.SpellDamage;
        if (def == null) return true;
        if (def.canHitAllies) return true;

        return TeamManager.Instance.AreEnemies(context.OwnerId, targetOwner);
    }
}
