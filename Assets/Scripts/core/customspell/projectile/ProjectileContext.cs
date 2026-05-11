using System;
using Unity.Netcode;

public class ProjectileContext : IProjectileContext {
    public SpellCaster Caster { get; }
    public ParticipantId OwnerId { get; }
    public SpellView View { get; }
    public ISpellTransform Movement { get; }
    public SpellDefinition Spell { get; }
    public SpellSystemEvent Event { get; }

    public bool Spawned { get; }
    public bool AlternativeSpawn { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public Action<SpellEvent> eventSink;

    public ProjectileContext(
        SpellCaster caster,
        SpellView view,
        ISpellTransform movement,
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
        Movement = movement;
        Spawned = spawned;
        AlternativeSpawn = alternativeSpawn;
        Lifetime = data.lifetime;
    }

    public void SendEvent(SpellEvent evt) {
        eventSink?.Invoke(evt);
    }
}