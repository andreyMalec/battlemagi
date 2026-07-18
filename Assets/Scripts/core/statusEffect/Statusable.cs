using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Statusable : MonoBehaviour {
    [SerializeField] private MonoBehaviour _bridge;

    private IStatusableBridge _bridgeTyped;

    private Dictionary<string, StatusEffectRuntime> _active = new();

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

        SpellLog.Log($"Adding effect {data.effectName} to {gameObject.name} from client {applyContext.ownerId}");
        if (FindPreviousEffect(data, out var previous)) {
            switch (data.CompareTo(previous.data)) {
                case EffectCompare.ResetTime:
                    previous.ResetTime();
                    break;
                case EffectCompare.Replace:
                    RemoveEffect(previous.data.effectName);
                    Apply(applyContext, data.onStack != null ? previous.data.onStack : data);
                    break;
                case EffectCompare.Add:
                    Apply(applyContext, data);
                    break;
            }
        } else {
            Apply(applyContext, data);
        }

        _bridgeTyped?.SyncFromCore(this);
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

        _bridgeTyped?.SyncFromCore(this);
        return removed;
    }

    public void HandleHit(DamageRequest hit) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        _bridgeTyped.HandleHit(hit);

        var toRemove = _active.Values
            .Where(e => e.data.removeOnHit)
            .Select(e => e.data.effectName)
            .ToList();

        for (var i = 0; i < toRemove.Count; i++)
            RemoveEffect(toRemove[i]);
    }

    internal void TickServer(float dt) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        if (_active.Count == 0) return;

        var snapshot = new List<StatusEffectRuntime>(_active.Values);
        var toRemove = new List<StatusEffectRuntime>();

        for (var i = 0; i < snapshot.Count; i++) {
            var effect = snapshot[i];
            effect.OnTick(gameObject, dt);
            if (effect.IsExpired)
                toRemove.Add(effect);
        }

        for (var i = 0; i < toRemove.Count; i++) {
            var expired = toRemove[i];
            RemoveEffect(expired.data.effectName);
            _bridgeTyped.HandleExpireChain(expired.OwnerId, expired.data);
        }


        _bridgeTyped?.SyncFromCore(this);
    }

    public struct DurationEffect {
        public Sprite icon;
        public float remains;
    }
}