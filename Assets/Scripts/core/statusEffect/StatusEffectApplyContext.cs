using UnityEngine;

public readonly struct StatusEffectApplyContext {
    public readonly ParticipantId ownerId;
    public ParticipantId OwnerId => ownerId;
    public readonly GameObject sourceObject;
    public readonly ulong sourceNetworkObjectId;
    public readonly float sourceProjectileInitialSpeed;

    public StatusEffectApplyContext(ParticipantId ownerId, GameObject sourceObject = null, ulong sourceNetworkObjectId = ulong.MaxValue, float sourceProjectileInitialSpeed = 0f) {
        this.ownerId = ownerId;
        this.sourceObject = sourceObject;
        this.sourceNetworkObjectId = sourceNetworkObjectId;
        this.sourceProjectileInitialSpeed = sourceProjectileInitialSpeed;
    }
}

