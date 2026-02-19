using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NetworkGameBootstrap : NetworkBehaviour, IAuthorityService {
    [SerializeField] private GameObject spellPrefab;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var (spellSystem, authority) = InitializeSpellSystem();
        GetComponent<SpellCaster>().Initialize(OwnerClientId, spellSystem, authority);
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = new NgoEntityManager(spellPrefab);
        IAuthorityService authority = this;
        NgoSpellSystemEvent spellSystemEvent = GetComponent<NgoSpellSystemEvent>();
        spellSystemEvent.Init(manager.SpellPrefab);

        var spellSystem = new SpellSystem(manager, authority, spellSystemEvent);
        Debug.Log(
            $" Network SpellSystem initialized with manager={manager}, authority={authority}, spellSystemEvent={spellSystemEvent}");

        DI.Register(manager);

        return (spellSystem, authority);
    }

    public GameObject Spawn(OwnerId ownerId, GameObject prefab, Vector3 pos, Quaternion rot) {
        var obj = Object.Instantiate(prefab, pos, rot);
        var networkObject = obj.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(ownerId.Value);
        return obj;
    }

    public void Destroy(GameObject go) {
        if (gameObject == null) return;
        var networkObject = gameObject.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned) {
            networkObject.Despawn();
        } else {
            Object.Destroy(gameObject);
        }
    }
}