using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NetworkGameBootstrap : NetworkBehaviour, IAuthorityService {
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var (spellSystem, authority) = InitializeSpellSystem();
        GetComponent<SpellCaster>().Initialize(OwnerClientId, spellSystem, authority);
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = new NgoEntityManager();
        IAuthorityService authority = this;
        NgoSpellSystemEvent spellSystemEvent = GetComponent<NgoSpellSystemEvent>();

        var spellSystem = new SpellSystem(authority, spellSystemEvent);
        Debug.Log(
            $" Network SpellSystem initialized with manager={manager}, authority={authority}, spellSystemEvent={spellSystemEvent}");

        DI.Register(manager);

        return (spellSystem, authority);
    }
}