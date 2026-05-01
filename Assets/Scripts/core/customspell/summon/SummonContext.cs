using System;
using Unity.VisualScripting;

public class SummonContext : ISpellContext {
    public SpellCaster Caster { get; }
    public OwnerId OwnerId { get; }
    public SpellView View { get; }

    public ISpellTransform Movement => throw new InvalidImplementationException();

    public SpellDefinition Spell { get; }
    public SpellSystemEvent Event { get; }

    public bool Spawned { get; }
    public bool AlternativeSpawn { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public Action<SpellEvent> eventSink;

    public SummonContext(
        SpellCaster caster,
        SpellView view,
        SpellDefinition data,
        SpellSystemEvent spellEvent,
        bool spawned,
        bool alternativeSpawn
    ) {
        Caster = caster;
        OwnerId = Caster.OwnerId;
        View = view;
        Spell = data;
        Event = spellEvent;
        Spawned = spawned;
        AlternativeSpawn = alternativeSpawn;
        Lifetime = data.lifetime;
    }

    public void SendEvent(SpellEvent evt) {
        eventSink?.Invoke(evt);
    }
}