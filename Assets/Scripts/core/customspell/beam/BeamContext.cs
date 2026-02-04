using System;
using Unity.Netcode;
using UnityEngine;

public class BeamContext : IBeamContext {
    public SpellRunner Caster { get; }
    public ulong OwnerId { get; }
    public SpellView View { get; }
    public ISpellTransform Movement { get; }
    public SpellDefinition Data { get; }

    public bool Spawned { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public Action<SpellEvent> eventSink;

    public Vector3 Origin => Caster.spawnPos.position;
    public Vector3 Direction => Caster.Direction;
    public float MaxLength => Data.beamMaxLength;

    public BeamContext(
        SpellRunner caster,
        SpellView view,
        ISpellTransform movement,
        SpellDefinition data,
        bool spawned
    ) {
        Caster = caster;
        OwnerId = Caster.GetComponent<NetworkObject>().OwnerClientId;
        View = view;
        Data = data;
        Movement = movement;
        Spawned = spawned;
        Lifetime = data.lifetime;
    }

    public void SendEvent(SpellEvent evt) {
        eventSink?.Invoke(evt);
    }
}

