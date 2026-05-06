using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NetworkGameBootstrap : NetworkBehaviour, IAuthorityService, SpellBootstrap {
    public OwnerId OwnerId => _logicalOwnerId ?? OwnerClientId;
    public ulong ObjectId => NetworkObjectId;

    private OwnerId? _logicalOwnerId;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var identity = GetComponent<ParticipantIdentity>();
        if (identity != null) {
            _logicalOwnerId = ParticipantOwnerCodec.Encode(identity.Id);
        }

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