using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NetworkGameBootstrap : NetworkBehaviour, IAuthorityService, SpellBootstrap {
    public OwnerId OwnerId => OwnerClientId;
    public ulong ObjectId => NetworkObjectId;

    public void Init(SpellCaster caster) {
        var (spellSystem, authority) = InitializeSpellSystem();
        caster?.Initialize(OwnerClientId, spellSystem, authority);
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = SpellPrefab.Instance;
        IAuthorityService authority = this;

        var spellSystem = new SpellSystem(authority);
        Debug.Log(
            $" Network SpellSystem initialized with manager={manager}, authority={authority}");

        DI.Register(manager);

        return (spellSystem, authority);
    }
}