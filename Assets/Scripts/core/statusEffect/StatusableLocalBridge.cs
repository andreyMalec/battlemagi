using System.Collections.Generic;
using UnityEngine;

public class StatusableLocalBridge : MonoBehaviour, IStatusableBridge {
    [SerializeField] private ulong clientId;

    private Statusable _core;
    private bool _hasStatusable;

    public bool IsServer => clientId == 0;
    public bool IsSpawned => true;
    public ulong OwnerId => clientId;
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

    public void HandleExpireChain(ulong ownerClientId, StatusEffectData expiredEffect) {
        if (expiredEffect != null && expiredEffect.onExpire != null)
            _core.AddEffect(ownerClientId, expiredEffect.onExpire);
    }

    public void HandleHit(DamageRequest hit) {
    }
}