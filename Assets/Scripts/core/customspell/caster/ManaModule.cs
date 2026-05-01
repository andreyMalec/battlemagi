using UnityEngine;

[System.Serializable]
public class ManaModule {
    [SerializeField] public float maxMana = 100f;
    [SerializeField] public float regenPerSecond = 5f;

    public float MaxMana => maxMana;
    public float RegenPerSecond => regenPerSecond;

    public float Mana { get; private set; }
    public float PrimalMana { get; private set; }

    private Stats _stats;

    public void InitializeServer(Stats stats) {
        PrimalMana = 0f;
        _stats = stats;
    }

    public void SetDefaults(float defaultMaxMana, float defaultRegenPerSecond) {
        maxMana = defaultMaxMana;
        Mana = maxMana;
        regenPerSecond = defaultRegenPerSecond;
    }

    public void SetNetworkState(float mana, float primalMana) {
        Mana = mana;
        PrimalMana = primalMana;
    }

    public void TickServer(float dt) {
        var regen = regenPerSecond * _stats?.GetFinal(StatType.ManaRegen) ?? 1f;

        RestoreManaServer(regen * dt);
    }

    public bool IsPrimalManaLocked(SpellDefinition spell, int echoCount) {
        if (spell != null && echoCount < spell.echoCount)
            return false;
        return PrimalMana > 0f;
    }

    public bool CanSpendForCast(SpellDefinition spell, int echoCount) {
        if (spell == null) return false;
        if (echoCount < spell.echoCount) return true;
        return Mana >= CostForCast(spell);
    }

    public bool CanSpendForChannelTick(SpellDefinition spell, float dt) {
        if (spell == null) return false;
        return Mana >= CostPerSecond(spell) * dt;
    }

    public float CostForCast(SpellDefinition spell) {
        if (spell == null) return 0f;
        var multi = _stats?.GetFinal(StatType.ManaCost) ?? 1f;
        return spell.manaCost * multi;
    }

    public float CostPerSecond(SpellDefinition spell) {
        var multi = _stats?.GetFinal(StatType.ManaCost) ?? 1f;
        return spell.manaPerSecond * multi;
    }

    public float PrimalManaMissing(float cost) {
        return Mathf.Max(0f, cost - Mana);
    }

    public bool SpendWithPrimalServer(float amount) {
        if (amount <= 0f) return true;

        var spend = Mathf.Min(Mana, amount);
        Mana -= spend;

        var missing = amount - spend;
        if (missing > 0f)
            PrimalMana += missing;

        return missing <= 0f;
    }

    public void RestoreManaServer(float amount) {
        if (amount <= 0f) return;

        Mana = Mathf.Clamp(Mana + amount, 0f, maxMana);
        PrimalMana = Mathf.Clamp(PrimalMana - amount, 0f, maxMana);
    }

    public void SpendManaServer(float amount) {
        Mana -= amount;
    }

    public void AddPrimalManaServer(float amount) {
        PrimalMana += amount;
    }
}