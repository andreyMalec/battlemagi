public interface ISpellCasterBridge: IdentityUser {
    bool IsServer { get; }
    bool IsSpawned { get; }
    bool IsOwner { get; }

    void Bind(SpellCasterPlayer core);
    void TickFixed(SpellCasterPlayer core);
    bool TrySpendMana(float amount);
    bool TrySpendHealth(float amount);
    void RestoreEcho(SpellDefinition spell, int amount);
    void BeginChanneling(SpellDefinition spell);
    void EndChanneling();
    void RequestStopChanneling();
    bool ShouldStopChanneling();
    void BindChannelingSpell(ulong spellObjectId, string spellName);
    void StopChannelingSpell(ulong spellObjectId);
}

