using UnityEngine;

public abstract class StatusEffectRuntime {
    public StatusEffectData data;
    protected float _timeRemaining;
    protected ulong _ownerClientId;

    public StatusEffectRuntime(StatusEffectData data) {
        this.data = data;
        _timeRemaining = data.duration;
    }

    public virtual void ResetTime() {
        _timeRemaining = data.duration;
    }

    public virtual void OnApply(ulong ownerClientId, GameObject target) {
        _ownerClientId = ownerClientId;
    }

    public virtual void OnExpire(GameObject target) {
    }

    public virtual void OnTick(GameObject target, float deltaTime) {
        _timeRemaining -= deltaTime;
    }

    public bool IsExpired => _timeRemaining <= 0;
}