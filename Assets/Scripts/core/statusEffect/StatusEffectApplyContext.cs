using UnityEngine;

public readonly struct StatusEffectApplyContext {
    public readonly ulong ownerClientId;
    public readonly GameObject sourceObject;
    public readonly ulong sourceNetworkObjectId;

    public StatusEffectApplyContext(ulong ownerClientId, GameObject sourceObject = null, ulong sourceNetworkObjectId = ulong.MaxValue) {
        this.ownerClientId = ownerClientId;
        this.sourceObject = sourceObject;
        this.sourceNetworkObjectId = sourceNetworkObjectId;
    }
}

