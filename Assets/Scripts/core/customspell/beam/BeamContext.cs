using System;
using Unity.Netcode;
using UnityEngine;

public class BeamContext : IBeamContext {
    public SpellCaster Caster { get; }
    public OwnerId OwnerId { get; }
    public SpellView View { get; }
    public ISpellTransform Movement { get; }
    public SpellDefinition Spell { get; }
    public SpellSystemEvent Event { get; }

    public bool Spawned { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public Action<SpellEvent> eventSink;

    public Vector3 Origin => Caster.Origin;
    public Vector3 Direction => Caster.Direction;
    public float MaxLength => Spell.beam.MaxLength;

    public BeamContext(
        SpellCaster caster,
        SpellView view,
        ISpellTransform movement,
        SpellDefinition data,
        SpellSystemEvent spellEvent,
        bool spawned
    ) {
        Caster = caster;
        OwnerId = Caster.OwnerId;
        View = view;
        Spell = data;
        Event = spellEvent;
        Movement = movement;
        Spawned = spawned;
        Lifetime = data.lifetime;
    }

    public void SendEvent(SpellEvent evt) {
        eventSink?.Invoke(evt);
    }
}

