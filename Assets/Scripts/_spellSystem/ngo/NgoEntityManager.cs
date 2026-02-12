using Unity.Netcode;
using UnityEngine;

public class NgoEntityManager : IEntityManager {
    public GameObject Spawn(OwnerId ownerId, GameObject prefab, Vector3 pos, Quaternion rot) {
        var obj = Object.Instantiate(prefab, pos, rot);
        var networkObject = obj.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(ownerId.Value);
        return obj;
    }

    public OwnerId Owner(GameObject obj) {
        return obj.GetComponent<NetworkObject>().OwnerClientId;
    }

    public bool IsOwner(GameObject obj) {
        return obj.GetComponent<NetworkObject>().IsOwner;
    }
}