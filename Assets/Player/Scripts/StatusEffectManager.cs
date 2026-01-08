using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class StatusEffectManager : NetworkBehaviour {
    private Dictionary<string, StatusEffectRuntime> activeEffects = new();
    public List<DurationEffect> ActiveEffects = new();

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
    }

    private void Update() {
        ActiveEffects = activeEffects.Values
            .Filter(it => it.data.icon != null && !it.IsExpired)
            .Map(it => new DurationEffect {
                icon = it.data.icon,
                remains = it._timeRemaining
            })
            .ToList();
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
    }

    public StatusEffectData RemoveEffect(string type) {
        StatusEffectData removed = null;
        foreach (var effect in activeEffects.Values.Where(effect => effect.data.effectName == type)) {
            effect.OnExpire(gameObject);
            removed = effect.data;
        }

        activeEffects = activeEffects.FilterKeys(effect => effect != type);

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

    public struct DurationEffect {
        public Sprite icon;
        public float remains;
    }
}