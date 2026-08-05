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
    private bool _pendingRebuild;
    private bool _syncRequested;
    private int _lastSyncedFrame = -1;
    private readonly List<NetDurationEffect> _snapshot = new();
    private readonly Dictionary<FixedString64Bytes, NetDurationEffect> _snapshotByName = new();
    private readonly Dictionary<FixedString64Bytes, int> _syncedIndexByName = new();
    private readonly List<Statusable.DurationEffect> _durationEffects = new();
    private float _nextTimeSyncAt;

    public ParticipantId OwnerId { get; set; }
    public List<Statusable.DurationEffect> DurationEffects => _durationEffects;

    private const float TimeSyncInterval = 0.2f;
    private const float RemainsSyncEpsilon = 0.05f;

    private void Awake() {
        _synced = new NetworkList<NetDurationEffect>();
        _onSyncedChanged = _ => _pendingRebuild = true;
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

    private void LateUpdate() {
        if (!_pendingRebuild) return;
        _pendingRebuild = false;
        RebuildActiveEffectsFromSynced();
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

        _syncRequested = true;
        TryFlushSync(core);
    }

    private void TryFlushSync(Statusable core) {
        if (!_syncRequested) return;
        if (_lastSyncedFrame == Time.frameCount) return;

        BuildSnapshot(core);
        var topologyChanged = IsTopologyChanged();
        if (!topologyChanged && Time.realtimeSinceStartup < _nextTimeSyncAt)
            return;

        _syncRequested = false;
        _lastSyncedFrame = Time.frameCount;
        _nextTimeSyncAt = Time.realtimeSinceStartup + TimeSyncInterval;

        ApplyDiff();
    }

    private void BuildSnapshot(Statusable core) {
        _snapshot.Clear();
        _snapshotByName.Clear();

        foreach (var effect in core.ActiveEffects) {
            if (effect.IsExpired) continue;
            var netEffect = new NetDurationEffect {
                effectName = effect.data.effectName,
                remains = effect._timeRemaining
            };
            _snapshot.Add(netEffect);
            _snapshotByName[netEffect.effectName] = netEffect;
        }
    }

    private bool IsTopologyChanged() {
        if (_synced.Count != _snapshot.Count)
            return true;

        for (var i = 0; i < _synced.Count; i++) {
            var existing = _synced[i];
            if (!_snapshotByName.ContainsKey(existing.effectName))
                return true;
        }

        return false;
    }

    private void ApplyDiff() {
        _syncedIndexByName.Clear();
        for (var i = 0; i < _synced.Count; i++) {
            var existing = _synced[i];
            _syncedIndexByName[existing.effectName] = i;
        }

        for (var i = _synced.Count - 1; i >= 0; i--) {
            var existing = _synced[i];
            if (_snapshotByName.ContainsKey(existing.effectName))
                continue;

            _synced.RemoveAt(i);
        }

        _syncedIndexByName.Clear();
        for (var i = 0; i < _synced.Count; i++) {
            var existing = _synced[i];
            _syncedIndexByName[existing.effectName] = i;
        }

        for (var i = 0; i < _snapshot.Count; i++) {
            var desired = _snapshot[i];
            if (!_syncedIndexByName.TryGetValue(desired.effectName, out var index)) {
                _synced.Add(desired);
                continue;
            }

            var current = _synced[index];
            if (Mathf.Abs(current.remains - desired.remains) < RemainsSyncEpsilon)
                continue;

            _synced[index] = desired;
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
                if (hit.fromId == ParticipantId.EnvironmentId) return;

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
        _durationEffects.Clear();
        var db = Ctx.StatusEffects.GetMap();
        try {
            for (var i = 0; i < _synced.Count; i++) {
                var e = _synced[i];
                if (!db.TryGetValue(e.effectName.ToString(), out var data))
                    continue;
                if (data.icon == null) continue;
                _durationEffects.Add(new Statusable.DurationEffect { icon = data.icon, remains = e.remains });
            }
        } catch (Exception ex) {
            Debug.LogError($"Exception while rebuilding synced effects: {ex}");
            _durationEffects.Clear();
            return;
        }
    }
}