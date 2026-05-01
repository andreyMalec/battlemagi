using System.Collections.Generic;

public interface IStatusableBridge {
    bool IsServer { get; }
    bool IsSpawned { get; }
    ulong OwnerId { get; }

    List<Statusable.DurationEffect> DurationEffects { get; }

    void Bind(Statusable core);
    void TickFixed(Statusable core);

    void SyncFromCore(Statusable core);
    void HandleExpireChain(ulong ownerClientId, StatusEffectData expiredEffect);
    void HandleHit(DamageRequest hit);
}