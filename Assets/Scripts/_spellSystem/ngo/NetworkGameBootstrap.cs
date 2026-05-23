using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NetworkGameBootstrap : NetworkBehaviour, IAuthorityService, SpellBootstrap, IdentityUser {
    public ParticipantId OwnerId { get; set; }
    public ulong ObjectId => NetworkObjectId;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        var caster = GetComponentInChildren<SpellCaster>();
        Init(caster);
    }

    public void Init(SpellCaster caster) {
        var (spellSystem, authority) = InitializeSpellSystem();
        caster?.Initialize(OwnerId, spellSystem, authority);
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = SpellPrefab.Instance;
        IAuthorityService authority = this;

        var spellSystem = new SpellSystem(authority);
        SpellLog.Log(
            $" Network SpellSystem initialized with manager={manager}, authority={authority}");

        DI.Register(manager);

        return (spellSystem, authority);
    }
}