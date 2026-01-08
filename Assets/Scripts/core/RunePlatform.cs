using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class RunePlatform : NetworkBehaviour {
    [SerializeField] private List<GameObject> runes;
    [SerializeField] private float restoreTime = 40f;
    [SerializeField] private float duration = 20f;
    [SerializeField] private Transform spawnPoint;

    private float _restoreTimer;
    private float _durationTimer;

    private void Update() {
        if (!IsServer) return;

        // Всегда тикаем таймер восстановления (как было раньше)
        _restoreTimer += Time.deltaTime;

        // проверяем, есть ли активная руна (дочерний объект с NetworkObject)
        NetworkObject activeNetObj = GetActiveRune();
        bool hasActive = activeNetObj != null && activeNetObj.IsSpawned;

        if (hasActive) {
            _durationTimer += Time.deltaTime;
            // истекло время жизни — удаляем
            if (_durationTimer >= duration) {
                DespawnRune(activeNetObj);
                _durationTimer = 0f;
            }
        } else {
            // когда рун нет — спавним сразу, если таймер восстановления истёк
            if (_restoreTimer >= restoreTime) {
                _restoreTimer = 0f;
                SpawnRune();
            }

            // нет активной — сбросить счетчик жизни, чтобы не накапливался
            _durationTimer = 0f;
        }
    }

    private void SpawnRune() {
        if (GetActiveRune() != null) return;
        var prefab = runes.Randomize();
        var runeObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var netObj = runeObj.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        var parentNetObj = GetComponent<NetworkObject>();
        if (parentNetObj != null && parentNetObj.IsSpawned) {
            netObj.TrySetParent(parentNetObj, worldPositionStays: true);
        }

        _durationTimer = 0f;
    }

    private void DespawnRune(NetworkObject netObj) {
        if (netObj == null) return;
        if (netObj.IsSpawned) {
            netObj.Despawn(true);
        }
    }

    private NetworkObject GetActiveRune() {
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            if (child.TryGetComponent(out NetworkObject netObj))
                return netObj;
        }

        return null;
    }

#if UNITY_EDITOR
    private GameObject prefab;

    private void OnDrawGizmos() {
        if (prefab == null)
            prefab = runes.Randomize();
        var filter = prefab.GetComponentInChildren<MeshFilter>();
        var obj = filter.gameObject;
        var m = filter.sharedMesh;
        Gizmos.DrawMesh(m, spawnPoint.transform.position, obj.transform.localRotation, obj.transform.localScale);
    }
#endif
}