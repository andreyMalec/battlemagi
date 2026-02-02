using Unity.Netcode;
using UnityEngine;

public class ZoneContext : IZoneContext {
    public SpellRunner Caster { get; }
    public ulong OwnerId { get; }
    public SpellView View { get; }

    public Vector3 Center => View.transform.position;
    public float Age { get; private set; }

    public float Time => UnityEngine.Time.time;
    public float DeltaTime => UnityEngine.Time.deltaTime;

    public ZoneContext(SpellRunner caster, SpellView view) {
        Caster = caster;
        View = view;
        OwnerId = Caster.GetComponent<NetworkObject>().OwnerClientId;
    }

    public void Tick(float delta) {
        Age += delta;
    }
}