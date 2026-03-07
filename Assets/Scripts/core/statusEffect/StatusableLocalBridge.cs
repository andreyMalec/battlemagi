using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Statusable))]
public class StatusableLocalBridge : MonoBehaviour, IStatusableBridge {
    [SerializeField] private ulong clientId;

    private Statusable _core;

    public bool IsServer => clientId == 0;
    public bool IsSpawned => true;
    public ulong OwnerId => clientId;
    public List<Statusable.DurationEffect> DurationEffects { get; } = new();

    private void Awake() {
        _core = GetComponent<Statusable>();
    }

    private void FixedUpdate() {
        TickFixed(_core);
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