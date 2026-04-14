using UnityEngine;

[System.Serializable]
public class HealthModule : IDamageModule {
    [Min(0f)] public float maxHealth = 100f;
    [Min(0f)] public float regenPerSecond = 1f;

    public float Health { get; private set; }

    private Damageable _damageable;
    private Stats _stats;

    public void Initialize(Damageable damageable, Stats stats) {
        _damageable = damageable;
        _stats = stats;
        Health = maxHealth;
    }

    public void SetDefaults(float defaultMaxHealth, float defaultRegenPerSecond) {
        maxHealth = defaultMaxHealth;
        Health = maxHealth;
        regenPerSecond = defaultRegenPerSecond;
    }

    public void TickServer(float dt) {
        if (!_damageable.IsAlive) return;
        if (regenPerSecond <= 0f) return;
        var regen = regenPerSecond * _stats?.GetFinal(StatType.HealthRegen) ?? 1f;
        Health = Mathf.Clamp(Health + regen * dt, 0f, maxHealth);
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
