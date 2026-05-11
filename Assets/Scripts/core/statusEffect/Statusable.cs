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
        if (_active.TryGetValue(data.effectName, out var previous)) {
            switch (data.CompareTo(previous.data)) {
                case StatusEffectData.RESET_TIME:
                    previous.ResetTime();
                    break;
                case StatusEffectData.REPLACE:
                    RemoveEffect(data.effectName);
                    Apply(applyContext, data);
                    break;
                case StatusEffectData.ADD:
                    Apply(applyContext, data);
                    break;
            }
        } else {
            Apply(applyContext, data);
        }

        _bridgeTyped?.SyncFromCore(this);
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