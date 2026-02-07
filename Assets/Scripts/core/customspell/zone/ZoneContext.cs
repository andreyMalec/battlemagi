using System;
using Unity.Netcode;

public class ZoneContext : IZoneContext {
    public SpellRunner Caster { get; }
    public ulong OwnerId { get; }
    public SpellView View { get; }
    public ISpellTransform Movement { get; }
    public SpellDefinition Spell { get; }

    public bool Spawned { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public Action<SpellEvent> eventSink;

    public ZoneContext(
        SpellRunner caster,
        SpellView view,
        ISpellTransform movement,
        SpellDefinition data,
        bool spawned
    ) {
        Caster = caster;
        OwnerId = 0;//Caster.GetComponent<NetworkObject>().OwnerClientId;
        View = view;
        Spell = data;
        Movement = movement;
        Spawned = spawned;
        Lifetime = data.lifetime;
    }

    public void SendEvent(SpellEvent evt) {
        eventSink?.Invoke(evt);
    }
}