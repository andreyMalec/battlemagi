using UnityEngine;

public abstract class StatusEffectRuntime {
    public StatusEffectData data;
    protected float timeRemaining;

    public StatusEffectRuntime(StatusEffectData data) {
        this.data = data;
        timeRemaining = data.duration;
    }

    public virtual void ResetTime() {
        timeRemaining = data.duration;
    }

    public virtual void OnApply(GameObject target) {
    }

    public virtual void OnExpire(GameObject target) {
    }

    public virtual void OnTick(GameObject target, float deltaTime) {
        timeRemaining -= deltaTime;
    }

    public bool IsExpired => timeRemaining <= 0;
}