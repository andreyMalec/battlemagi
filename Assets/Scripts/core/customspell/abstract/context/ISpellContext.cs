public interface ISpellContext {
    SpellCaster Caster { get; }
    SpellView View { get; }
    ISpellTransform Movement { get; }
    SpellDefinition Spell { get; }
    SpellSystemEvent Event { get; }
    StatSystem Stats => View.Stats.System;

    DamageDefinition SpellDamage => Spell.damage;

    bool Spawned { get; }

    float Lifetime { get; }

    OwnerId OwnerId { get; }
    float Time { get; }
    float DeltaTime { get; }

    void SendEvent(SpellEvent evt);
}