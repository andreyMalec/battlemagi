using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class StatusableNetworkBridge : NetworkBehaviour, IStatusableBridge {
    private struct NetDurationEffect : INetworkSerializable, IEquatable<NetDurationEffect> {
        public FixedString64Bytes effectName;
        public float remains;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref effectName);
            serializer.SerializeValue(ref remains);
        }

        public bool Equals(NetDurationEffect other) {
            return effectName.Equals(other.effectName) && remains.Equals(other.remains);
        }
    }

    private Statusable _core;
    private bool _hasCore;
    private Stats _stats;

    private NetworkList<NetDurationEffect> _synced;
    private NetworkList<NetDurationEffect>.OnListChangedDelegate _onSyncedChanged;

    public ParticipantId OwnerId { get; set; }
    public List<Statusable.DurationEffect> DurationEffects { get; private set; } = new();

    private void Awake() {
        _synced = new NetworkList<NetDurationEffect>();
        _onSyncedChanged = _ => RebuildActiveEffectsFromSynced();
        _stats = GetComponent<Stats>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _synced.OnListChanged += _onSyncedChanged;
        RebuildActiveEffectsFromSynced();
        if (IsServer)
            SyncFromCore(_core);
    }

    public override void OnNetworkDespawn() {
        _synced.OnListChanged -= _onSyncedChanged;
        base.OnNetworkDespawn();
    }

    private void FixedUpdate() {
        if (!_hasCore) return;
        TickFixed(_core);
    }

    public void Bind(Statusable core) {
        _core = GetComponentInChildren<Statusable>();
        _hasCore = true;
        if (IsServer)
            SyncFromCore(_core);
    }

    public void TickFixed(Statusable core) {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;

        core.TickServer(Time.fixedDeltaTime);
        SyncFromCore(core);
    }

    public void SyncFromCore(Statusable core) {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;

        _synced.Clear();
        foreach (var effect in core.ActiveEffects) {
            if (effect.IsExpired) continue;
            _synced.Add(new NetDurationEffect {
                effectName = effect.data.effectName,
                remains = effect._timeRemaining
            });
        }
    }

    public void HandleExpireChain(ParticipantId ownerId, StatusEffectData expiredEffect) {
        if (!IsServer) return;
        if (!IsSpawned) return;

        if (expiredEffect != null && expiredEffect.onExpire != null)
            _core.AddEffect(ownerId, expiredEffect.onExpire);
    }

    public void HandleHit(DamageRequest hit) {
        if (hit.source != "Pain Mirror" && hit.fromId != OwnerId) {
            if (_core.HasEffect("Pain Mirror")) {
                if (ParticipantIdentity.TryFind(hit.fromId, out var player) && player != null) {
                    var reflectDamage = hit.amount;
                    if (_stats != null)
                        reflectDamage *= _stats.GetFinal(StatType.DamageReflection);

                    player.GetComponent<Damageable>()
                        .TakeDamage("Pain Mirror", OwnerId, reflectDamage, DamageKind.Reflect,
                            true);
                }
            }
        }
    }

    private void RebuildActiveEffectsFromSynced() {
        var list = new List<Statusable.DurationEffect>();
        var db = StatusEffectDatabase.Instance.GetMap();
        foreach (var e in _synced) {
            if (!db.TryGetValue(e.effectName.ToString(), out var data))
                continue;
            if (data.icon == null) continue;
            list.Add(new Statusable.DurationEffect { icon = data.icon, remains = e.remains });
        }

        DurationEffects = list;
    }
}