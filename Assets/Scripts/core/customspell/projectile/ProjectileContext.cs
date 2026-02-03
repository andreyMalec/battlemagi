using Unity.Netcode;
using UnityEngine;

public class ProjectileContext : IProjectileContext {
    public SpellRunner Caster { get; }
    public ulong OwnerId { get; }
    public SpellView View { get; }
    public ISpellTransform Movement { get; }
    public SpellDefinition Data { get; }

    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public ProjectileContext(
        SpellRunner caster,
        SpellView view,
        ISpellTransform movement,
        SpellDefinition data
    ) {
        Caster = caster;
        OwnerId = Caster.GetComponent<NetworkObject>().OwnerClientId;
        View = view;
        Data = data;
        Movement = movement;
        Lifetime = data.lifetime;
    }
}