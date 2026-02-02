using Unity.Netcode;
using UnityEngine;

public class ZoneContext : IZoneContext {
    public SpellRunner Caster { get; }
    public ulong OwnerId { get; }
    public SpellView View { get; }
    public SpellDefinition Data { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public ZoneContext(
        SpellRunner caster,
        SpellView view,
        SpellDefinition data
    ) {
        Caster = caster;
        OwnerId = Caster.GetComponent<NetworkObject>().OwnerClientId;
        View = view;
        Data = data;
        Lifetime = data.lifetime;
    }
}