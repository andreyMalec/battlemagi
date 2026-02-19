using Unity.Netcode;
using UnityEngine;

public class NgoEntityManager : IEntityManager {
    public GameObject SpellPrefab { get; }

    public NgoEntityManager(GameObject spellPrefab) {
        SpellPrefab = spellPrefab;
    }

    public GameObject Spawn(OwnerId ownerId, GameObject prefab, Vector3 pos, Quaternion rot) {
        var obj = Object.Instantiate(prefab, pos, rot);
        var networkObject = obj.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(ownerId.Value);
        return obj;
    }

    public void Destroy(GameObject gameObject) {
        if (gameObject == null) return;
        var networkObject = gameObject.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned) {
            networkObject.Despawn();
        } else {
            Object.Destroy(gameObject);
        }
    }
}