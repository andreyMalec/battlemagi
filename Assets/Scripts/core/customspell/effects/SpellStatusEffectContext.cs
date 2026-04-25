using Unity.Netcode;

public static class SpellStatusEffectContext {
    public static StatusEffectApplyContext Create(ISpellContext context) {
        var sourceObject = context.View.transform.parent.gameObject;
        var sourceNetworkObjectId = ulong.MaxValue;
        var sourceProjectileInitialSpeed = 0f;

        if (sourceObject.TryGetComponent<NetworkObject>(out var networkObject))
            sourceNetworkObjectId = networkObject.NetworkObjectId;

        if (context.Spell.coreType == CoreType.Projectile)
            sourceProjectileInitialSpeed = context.Spell.projectile.moveSpeed;
        if (context.Spell.coreType == CoreType.Zone)
            sourceProjectileInitialSpeed = context.Spell.zone.moveSpeed;
        if (context.Spell.coreType == CoreType.Beam)
            sourceProjectileInitialSpeed = context.Spell.beam.moveSpeed;

        return new StatusEffectApplyContext(
            context.OwnerId,
            sourceObject,
            sourceNetworkObjectId,
            sourceProjectileInitialSpeed * context.Stats.GetFinal(StatType.ProjectileSpeed));
    }
}