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

        _restoreTimer += Time.deltaTime;

        // проверяем, есть ли активная руна (дочерний объект с NetworkObject)
        NetworkObject activeNetObj = GetActiveRune();

        if (activeNetObj != null && activeNetObj.IsSpawned) {
            _durationTimer += Time.deltaTime;

            // истекло время жизни — удаляем
            if (_durationTimer >= duration) {
                DespawnRune(activeNetObj);
                _durationTimer = 0f;
            }
        } else {
            // если руны нет, но пришло время спавна
            if (_restoreTimer >= restoreTime) {
                _restoreTimer = 0f;
                SpawnRune();
            }

            // сбрасываем duration, чтобы не накапливалось
            _durationTimer = 0f;
        }
    }

    private void SpawnRune() {
        var prefab = runes.Randomize();
        var runeObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);

        var netObj = runeObj.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        netObj.TrySetParent(GetComponent<NetworkObject>());

        _durationTimer = 0f;
    }

    private void DespawnRune(NetworkObject netObj) {
        if (netObj == null) return;

        if (netObj.TryGetComponent(out Rune rune)) {
            rune.DestroyClientRpc(netObj.NetworkObjectId);
        }
    }

    private NetworkObject GetActiveRune() {
        // можно ориентироваться на первого ребёнка
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            if (child.TryGetComponent(out NetworkObject netObj))
                return netObj;
        }

        return null;
    }
}