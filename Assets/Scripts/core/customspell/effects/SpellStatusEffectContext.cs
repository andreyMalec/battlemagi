using Unity.Netcode;

public static class SpellStatusEffectContext {
    public static StatusEffectApplyContext Create(ISpellContext context) {
        var sourceObject = context.View.transform.parent.gameObject;
        var sourceNetworkObjectId = ulong.MaxValue;

        if (sourceObject.TryGetComponent<NetworkObject>(out var networkObject))
            sourceNetworkObjectId = networkObject.NetworkObjectId;

        return new StatusEffectApplyContext(context.OwnerId, sourceObject, sourceNetworkObjectId);
    }
}

