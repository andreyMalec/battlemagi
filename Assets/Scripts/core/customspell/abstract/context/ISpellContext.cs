public interface ISpellContext {
    SpellCaster Caster { get; }
    SpellView View { get; }
    ISpellTransform Movement { get; }
    SpellDefinition Spell { get; }
    SpellSystemEvent Event => Caster.SpellSystemEvent;

    bool Spawned { get; }

    float Lifetime { get; }

    OwnerId OwnerId { get; }
    float Time { get; }
    float DeltaTime { get; }

    void SendEvent(SpellEvent evt);
}