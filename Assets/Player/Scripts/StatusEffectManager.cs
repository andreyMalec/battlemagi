using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class StatusEffectManager : NetworkBehaviour {
    private struct NetDurationEffect : INetworkSerializable, System.IEquatable<NetDurationEffect> {
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

    private Dictionary<string, StatusEffectRuntime> activeEffects = new();
    public List<DurationEffect> ActiveEffects = new();

    private NetworkList<NetDurationEffect> _synced;
    private bool _loggedMissingCatalog;
    private NetworkList<NetDurationEffect>.OnListChangedDelegate _onSyncedChanged;

    private void Awake() {
        _synced = new NetworkList<NetDurationEffect>();
        _onSyncedChanged = _ => RebuildActiveEffectsFromSynced();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _synced.OnListChanged += _onSyncedChanged;
        RebuildActiveEffectsFromSynced();
    }

    public override void OnNetworkDespawn() {
        _synced.OnListChanged -= _onSyncedChanged;
        base.OnNetworkDespawn();
    }

    void FixedUpdate() {
        if (!IsServer) return;

        var toRemove = new List<string>();
        var toAdd = new List<KeyValuePair<ulong, StatusEffectData>>();
        foreach (var effect in activeEffects.Values) {
            effect.OnTick(gameObject, Time.deltaTime);
            if (effect.IsExpired) {
                effect.OnExpire(gameObject);
                toRemove.Add(effect.data.effectName);
                if (effect.data.onExpire != null) {
                    toAdd.Add(new KeyValuePair<ulong, StatusEffectData>(effect.ownerClientId, effect.data.onExpire));
                }
            }
        }

        foreach (var effect in toRemove) {
            activeEffects.Remove(effect);
        }

        foreach (var effect in toAdd) {
            AddEffect(effect.Key, effect.Value);
        }

        SyncForClients();
    }

    public void HandleHit() {
        foreach (var effect in activeEffects.Values.Where(effect => effect.data.removeOnHit)) {
            RemoveEffect(effect.data.effectName);
        }
    }

    public void AddEffect(ulong ownerClientId, StatusEffectData effect) {
        if (activeEffects.TryGetValue(effect.effectName, out var previous)) {
            switch (effect.CompareTo(previous.data)) {
                case StatusEffectData.RESET_TIME:
                    previous.ResetTime();
                    break;
                case StatusEffectData.REPLACE:
                    previous.OnExpire(gameObject);
                    Apply(ownerClientId, effect);
                    break;
                case StatusEffectData.ADD:
                    Apply(ownerClientId, effect);
                    break;
            }
        } else {
            Apply(ownerClientId, effect);
        }

        if (IsServer)
            SyncForClients();
    }

    public StatusEffectData RemoveEffect(string type) {
        StatusEffectData removed = null;
        foreach (var effect in activeEffects.Values.Where(effect => effect.data.effectName == type)) {
            effect.OnExpire(gameObject);
            removed = effect.data;
        }

        activeEffects = activeEffects.FilterKeys(effect => effect != type);

        if (IsServer)
            SyncForClients();

        return removed;
    }

    private void Apply(ulong ownerClientId, StatusEffectData effect) {
        var runtime = effect.CreateRuntime();
        runtime.OnApply(ownerClientId, gameObject);
        activeEffects[effect.effectName] = runtime;
        Debug.Log($"AddEffect {effect.effectName} to {gameObject.name}");
    }

    public bool HasEffect(string type) {
        return activeEffects.ContainsKey(type);
    }

    private void SyncForClients() {
        _synced.Clear();
        foreach (var effect in activeEffects.Values) {
            if (effect.IsExpired) continue;
            _synced.Add(new NetDurationEffect {
                effectName = effect.data.effectName,
                remains = effect._timeRemaining
            });
        }
    }

    private void RebuildActiveEffectsFromSynced() {
        var list = new List<DurationEffect>();
        var db = StatusEffectDatabase.Instance.GetMap();
        foreach (var e in _synced) {
            if (!db.TryGetValue(e.effectName.ToString(), out var data))
                continue;
            if (data.icon == null) continue;
            list.Add(new DurationEffect { icon = data.icon, remains = e.remains });
        }

        ActiveEffects = list;
    }

    public struct DurationEffect {
        public Sprite icon;
        public float remains;
    }
}