using UnityEngine;

public readonly struct StatusEffectApplyContext {
    public readonly ulong ownerClientId;
    public readonly GameObject sourceObject;
    public readonly ulong sourceNetworkObjectId;
    public readonly float sourceProjectileInitialSpeed;

    public StatusEffectApplyContext(ulong ownerClientId, GameObject sourceObject = null, ulong sourceNetworkObjectId = ulong.MaxValue, float sourceProjectileInitialSpeed = 0f) {
        this.ownerClientId = ownerClientId;
        this.sourceObject = sourceObject;
        this.sourceNetworkObjectId = sourceNetworkObjectId;
        this.sourceProjectileInitialSpeed = sourceProjectileInitialSpeed;
    }
}

