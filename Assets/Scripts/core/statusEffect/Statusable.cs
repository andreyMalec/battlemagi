using System;
using System.Collections.Generic;
using UnityEngine;

public class Statusable : MonoBehaviour {
    [SerializeField] private MonoBehaviour _bridge;

    private IStatusableBridge _bridgeTyped;

    private Dictionary<string, StatusEffectRuntime> _active = new();
    private readonly List<string> _removeOnHitBuffer = new();
    private readonly List<StatusEffectRuntime> _tickSnapshot = new();
    private readonly List<StatusEffectRuntime> _tickToRemove = new();
    private int _syncBatchDepth;
    private bool _syncPending;

    public event Action<StatusEffectRuntime> OnAdded;
    public event Action<StatusEffectRuntime> OnRemoved;

    internal IReadOnlyCollection<StatusEffectRuntime> ActiveEffects => _active.Values;

    public List<DurationEffect> DurationEffects => _bridgeTyped.DurationEffects;
    public ParticipantId OwnerId => _bridgeTyped.OwnerId;

    private void Awake() {
        if (_bridge != null)
            _bridgeTyped = (IStatusableBridge)_bridge;
        else
            _bridgeTyped = GetComponentInParent<IStatusableBridge>();
        _bridgeTyped.Bind(this);
    }

    public bool HasEffect(string effectName) {
        return _active.ContainsKey(effectName);
    }

    public void AddEffect(ParticipantId ownerId, StatusEffectData data) {
        AddEffect(new StatusEffectApplyContext(ownerId), data);
    }

    public void AddEffect(StatusEffectApplyContext applyContext, StatusEffectData data) {
        if (data == null) return;

        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;

        var changed = false;
        BeginSyncBatch();
        try {
            if (GameConfig.SpellDebugLogsEnabled)
                SpellLog.Log($"Adding effect {data.effectName} to {gameObject.name} from client {applyContext.ownerId}");

            if (FindPreviousEffect(data, out var previous)) {
                switch (data.CompareTo(previous.data)) {
                    case EffectCompare.ResetTime:
                        previous.ResetTime();
                        changed = true;
                        break;
                    case EffectCompare.Replace:
                        RemoveEffect(previous.data.effectName);
                        Apply(applyContext, data.onStack != null ? previous.data.onStack : data);
                        changed = true;
                        break;
                    case EffectCompare.Add:
                        Apply(applyContext, data);
                        changed = true;
                        break;
                }
            } else {
                Apply(applyContext, data);
                changed = true;
            }

            if (changed)
                RequestSync();
        } finally {
            EndSyncBatch();
        }
    }

    private bool FindPreviousEffect(StatusEffectData data, out StatusEffectRuntime previous) {
        if (data.onStack == null) {
            return _active.TryGetValue(data.effectName, out previous);
        }

        foreach (var v in _active.Values) {
            if (v.data == data || v.data == data.onStack || v.data == data.onStack.onStack ||
                v.data == data.onStack.onStack.onStack || v.data == data.onStack.onStack.onStack.onStack) {
                previous = v;
                return true;
            }
        }

        previous = null;
        return false;
    }

    private void Apply(StatusEffectApplyContext applyContext, StatusEffectData data) {
        var runtime = data.CreateRuntime();
        runtime.OnApply(applyContext, gameObject);
        _active[data.effectName] = runtime;
        OnAdded?.Invoke(runtime);
    }

    public StatusEffectData RemoveEffect(string effectName) {
        if (!_bridgeTyped.IsServer) return null;
        if (!_bridgeTyped.IsSpawned) return null;

        if (!_active.TryGetValue(effectName, out var runtime))
            return null;

        runtime.OnExpire(gameObject);
        var removed = runtime.data;
        _active.Remove(effectName);
        OnRemoved?.Invoke(runtime);

        RequestSync();
        return removed;
    }

    public void HandleHit(DamageRequest hit) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        _bridgeTyped.HandleHit(hit);

        _removeOnHitBuffer.Clear();
        foreach (var effect in _active.Values) {
            if (!effect.data.removeOnHit)
                continue;

            _removeOnHitBuffer.Add(effect.data.effectName);
        }

        if (_removeOnHitBuffer.Count == 0)
            return;

        BeginSyncBatch();
        try {
            for (var i = 0; i < _removeOnHitBuffer.Count; i++)
                RemoveEffect(_removeOnHitBuffer[i]);
        } finally {
            EndSyncBatch();
        }
    }

    internal void TickServer(float dt) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        if (_active.Count == 0) return;

        _tickSnapshot.Clear();
        _tickToRemove.Clear();
        foreach (var effect in _active.Values)
            _tickSnapshot.Add(effect);

        for (var i = 0; i < _tickSnapshot.Count; i++) {
            var effect = _tickSnapshot[i];
            effect.OnTick(gameObject, dt);
            if (effect.IsExpired)
                _tickToRemove.Add(effect);
        }

        if (_tickToRemove.Count == 0)
            return;

        BeginSyncBatch();
        try {
            for (var i = 0; i < _tickToRemove.Count; i++) {
                var expired = _tickToRemove[i];
                RemoveEffect(expired.data.effectName);
                _bridgeTyped.HandleExpireChain(expired.OwnerId, expired.data);
            }
        } finally {
            EndSyncBatch();
        }
    }

    private void RequestSync() {
        if (_syncBatchDepth > 0) {
            _syncPending = true;
            return;
        }

        _bridgeTyped?.SyncFromCore(this);
    }

    private void BeginSyncBatch() {
        _syncBatchDepth++;
    }

    private void EndSyncBatch() {
        if (_syncBatchDepth == 0)
            return;

        _syncBatchDepth--;
        if (_syncBatchDepth != 0)
            return;

        if (!_syncPending)
            return;

        _syncPending = false;
        _bridgeTyped?.SyncFromCore(this);
    }

    public struct DurationEffect {
        public Sprite icon;
        public float remains;
    }
}