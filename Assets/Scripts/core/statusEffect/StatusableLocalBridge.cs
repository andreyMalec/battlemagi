using System.Collections.Generic;
using UnityEngine;

public class StatusableLocalBridge : MonoBehaviour, IStatusableBridge {
    [SerializeField] private ParticipantKind ownerKind = ParticipantKind.Human;
    [SerializeField] private ulong ownerValue;

    private Statusable _core;
    private bool _hasStatusable;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public ParticipantId OwnerId {
        get => new ParticipantId(ownerKind, ownerValue);
        set => throw new System.NotImplementedException();
    }

    public List<Statusable.DurationEffect> DurationEffects { get; } = new();

    private void FixedUpdate() {
        if (!_hasStatusable) return;
        TickFixed(_core);
    }

    public void Bind(Statusable core) {
        _core = core;
        _hasStatusable = true;
    }

    public void TickFixed(Statusable core) {
        core.TickServer(Time.fixedDeltaTime);
    }

    public void SyncFromCore(Statusable core) {
    }

    public void HandleExpireChain(ParticipantId ownerId, StatusEffectData expiredEffect) {
        if (expiredEffect != null && expiredEffect.onExpire != null)
            _core.AddEffect(ownerId, expiredEffect.onExpire);
    }

    public void HandleHit(DamageRequest hit) {
    }
}