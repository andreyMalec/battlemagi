using Unity.Netcode;
using UnityEngine;

public class ProjectileContext : IProjectileContext {
    public SpellRunner Caster { get; }
    public ulong OwnerId { get; }
    public SpellView View { get; }

    public Vector3 Position {
        get => View.transform.position;
        set => View.transform.position = value;
    }

    public Vector3 Velocity { get; set; }
    public float Lifetime { get; set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public ProjectileContext(
        SpellRunner caster,
        SpellView view,
        Vector3 velocity,
        float lifetime
    ) {
        Caster = caster;
        OwnerId = Caster.GetComponent<NetworkObject>().OwnerClientId;
        View = view;
        Velocity = velocity;
        Lifetime = lifetime;
    }
}