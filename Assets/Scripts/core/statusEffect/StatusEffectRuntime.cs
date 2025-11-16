using UnityEngine;

public abstract class StatusEffectRuntime {
    public StatusEffectData data;
    public ulong ownerClientId;
    protected float _timeRemaining;

    public StatusEffectRuntime(StatusEffectData data) {
        this.data = data;
        _timeRemaining = data.duration;
    }

    public virtual void ResetTime() {
        _timeRemaining = data.duration;
    }

    public virtual void OnApply(ulong ownerClientId, GameObject target) {
        this.ownerClientId = ownerClientId;
        if (target.TryGetComponent<Player>(out var player))
            player.ApplyEffectColorClientRpc(data.color);
    }

    public virtual void OnExpire(GameObject target) {
        if (target.TryGetComponent<Player>(out var player))
            player.RemoveEffectColorClientRpc(data.color);
    }

    public virtual void OnTick(GameObject target, float deltaTime) {
        _timeRemaining -= deltaTime;
    }

    public bool IsExpired => _timeRemaining <= 0;
}