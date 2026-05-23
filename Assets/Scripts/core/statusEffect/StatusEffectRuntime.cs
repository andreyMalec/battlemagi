using UnityEngine;

public abstract class StatusEffectRuntime {
    public StatusEffectData data;
    public ParticipantId OwnerId;
    public float _timeRemaining;

    public StatusEffectRuntime(StatusEffectData data) {
        this.data = data;
        _timeRemaining = data.duration;
    }

    public virtual void ResetTime() {
        _timeRemaining = data.duration;
    }

    public virtual void OnApply(StatusEffectApplyContext applyContext, GameObject target) {
        OnApply(applyContext.ownerId, target);
    }

    public virtual void OnApply(ParticipantId ownerId, GameObject target) {
        this.OwnerId = ownerId;
        if (target.TryGetComponent<Colorable>(out var player))
            player.ApplyEffectColorClientRpc(data.color);
    }

    public virtual void OnExpire(GameObject target) {
        if (target.TryGetComponent<Colorable>(out var player))
            player.RemoveEffectColorClientRpc(data.color);
    }

    public virtual void OnTick(GameObject target, float deltaTime) {
        _timeRemaining -= deltaTime;
    }

    public bool IsExpired => _timeRemaining <= 0;
}