using System;
using Unity.Netcode;
using Unity.VisualScripting;

public class SummonContext : ISpellContext {
    public SpellCaster Caster { get; }
    public OwnerId OwnerId { get; }
    public SpellView View { get; }

    public ISpellTransform Movement => throw new InvalidImplementationException();

    public SpellDefinition Spell { get; }
    public SpellSystemEvent Event { get; }

    public bool Spawned { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public Action<SpellEvent> eventSink;

    public SummonContext(
        SpellCaster caster,
        SpellView view,
        SpellDefinition data,
        SpellSystemEvent spellEvent,
        bool spawned
    ) {
        Caster = caster;
        OwnerId = Caster.OwnerId;
        View = view;
        Spell = data;
        Event = spellEvent;
        Spawned = spawned;
        Lifetime = data.lifetime;
    }

    public void SendEvent(SpellEvent evt) {
        eventSink?.Invoke(evt);
    }

    public DamageDefinition SpellDamage {
        get {
            var d = Spell.damage;
            if (d != null) return d;

            var f = Spell.GetType().GetField("damage");
            if (f != null) return f.GetValue(Spell) as DamageDefinition;

            var p = Spell.GetType().GetProperty("damage");
            if (p != null) return p.GetValue(Spell) as DamageDefinition;

            return null;
        }
    }
}