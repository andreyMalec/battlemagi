public interface ISpellCasterBridge {
    bool IsServer { get; }
    bool IsSpawned { get; }
    bool IsOwner { get; }

    ulong OwnerId { get; }

    void Bind(SpellCasterPlayer core);
    void TickFixed(SpellCasterPlayer core);
    bool TrySpendMana(float amount);
    bool TrySpendHealth(float amount);
    void RestoreEcho(SpellDefinition spell, int amount);
}

