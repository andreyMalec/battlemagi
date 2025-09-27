using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StatusEffectManager : NetworkBehaviour {
    private Dictionary<System.Type, StatusEffectRuntime> activeEffects = new();

    void Update() {
        if (!IsServer) return;

        var toRemove = new List<System.Type>();
        foreach (var effect in activeEffects.Values) {
            effect.OnTick(gameObject, Time.deltaTime);
            if (effect.IsExpired) {
                effect.OnExpire(gameObject);
                toRemove.Add(effect.data.GetType());
            }
        }

        foreach (var effect in toRemove) {
            activeEffects.Remove(effect);
        }
    }

    public void AddEffect(ulong ownerClientId, StatusEffectData effect) {
        if (activeEffects.TryGetValue(effect.GetType(), out var previous)) {
            switch (effect.CompareTo(previous.data)) {
                case 0:
                    previous.ResetTime();
                    break;
                case 1:
                    previous.OnExpire(gameObject);
                    Apply(ownerClientId, effect);
                    break;
            }
        } else {
            Apply(ownerClientId, effect);
        }
    }

    private void Apply(ulong ownerClientId, StatusEffectData effect) {
        var runtime = effect.CreateRuntime();
        runtime.OnApply(ownerClientId, gameObject);
        activeEffects[effect.GetType()] = runtime;
        Debug.Log($"AddEffect {effect.effectName} to {gameObject.name}");
    }

    public bool HasEffect(System.Type type) {
        return activeEffects.ContainsKey(type);
    }
}