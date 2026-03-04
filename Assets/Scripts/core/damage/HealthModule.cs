using UnityEngine;

[System.Serializable]
public class HealthModule : IDamageModule {
    [Min(0f)] public float maxHealth = 100f;
    [Min(0f)] public float regenPerSecond = 1f;

    public float Health { get; private set; }

    private Damageable _damageable;

    public void Initialize(Damageable damageable) {
        _damageable = damageable;
        Health = maxHealth;
    }

    public void TickServer(float dt) {
        if (!_damageable.IsAlive) return;
        if (regenPerSecond <= 0f) return;
        Health = Mathf.Clamp(Health + regenPerSecond * dt, 0f, maxHealth);
    }

    public float ApplyDamage(float amount) {
        var applied = Mathf.Min(Health, amount);
        Health -= applied;
        return applied;
    }

    public void ApplyHeal(float amount) {
        if (amount <= 0f) return;
        Health = Mathf.Clamp(Health + amount, 0f, maxHealth);
    }

    public void SetHealthServer(float value) {
        Health = Mathf.Clamp(value, 0f, maxHealth);
    }
}
