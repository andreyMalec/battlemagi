using System;
using Unity.Netcode;
using UnityEngine;

public class SpellPrefab : MonoBehaviour, IEntityManager {
    public GameObject spellPrefabLocal;
    public GameObject spellPrefabNetwork;

    public static SpellPrefab Instance;

    public GameObject GetPrefab(bool useNetwork) {
        return useNetwork ? spellPrefabNetwork : spellPrefabLocal;
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }
    
    public GameObject Spawn(OwnerId ownerId, GameObject prefab, Vector3 pos, Quaternion rot) {
        var obj = Instantiate(prefab, pos, rot);
        var networkObject = obj.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(ownerId.Value);
        return obj;
    }

    public void Despawn(GameObject go) {
        if (go == null) return;
        var networkObject = go.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned) {
            networkObject.Despawn();
        } else {
            Destroy(go);
        }
    }
}