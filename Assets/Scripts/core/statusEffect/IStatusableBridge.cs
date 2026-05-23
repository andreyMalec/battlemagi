using System.Collections.Generic;

public interface IStatusableBridge:IdentityUser {
    bool IsServer { get; }
    bool IsSpawned { get; }

    List<Statusable.DurationEffect> DurationEffects { get; }

    void Bind(Statusable core);
    void TickFixed(Statusable core);

    void SyncFromCore(Statusable core);
    void HandleExpireChain(ParticipantId ownerId, StatusEffectData expiredEffect);
    void HandleHit(DamageRequest hit);
}