using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PickUpSpawn : NetworkBehaviour {
    [SerializeField] private List<PickUp> pickUps;
    [SerializeField] private float restoreTime = 15f;
    [SerializeField] private bool spawnAtStart = false;
    [SerializeField] private Transform spawnPoint;

    private float _restoreTimer;

    private void Awake() {
        if (spawnAtStart) {
            _restoreTimer += restoreTime;
        }
    }

    private void Update() {
        if (!IsServer) return;

        var activeNetObj = GetActiveItem();
        var hasActive = activeNetObj != null && activeNetObj.IsSpawned;

        if (hasActive) return;
        _restoreTimer += Time.deltaTime;
        if (_restoreTimer >= restoreTime) {
            _restoreTimer = 0f;
            SpawnItem();
        }
    }

    private void SpawnItem() {
        if (GetActiveItem() != null) return;
        var prefab = pickUps.Randomize();
        var obj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var netObj = obj.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        var parentNetObj = GetComponent<NetworkObject>();
        if (parentNetObj != null && parentNetObj.IsSpawned) {
            netObj.TrySetParent(parentNetObj, worldPositionStays: true);
        }
    }

    private NetworkObject GetActiveItem() {
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            if (child.TryGetComponent(out NetworkObject netObj))
                return netObj;
        }

        return null;
    }

    private void OnDrawGizmos() {
        if (pickUps == null || pickUps.Count == 0) return;
        var prefab = pickUps.Randomize();
        if (prefab == null) return;
        var filter = prefab.GetComponentInChildren<MeshFilter>();
        var obj = filter.gameObject;
        var m = filter.sharedMesh;
        Gizmos.DrawMesh(m, spawnPoint.transform.position, transform.localRotation * obj.transform.localRotation,
            obj.transform.localScale);
    }
}