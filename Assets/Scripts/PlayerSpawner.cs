using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour {
    public GameObject player;

    private void Start() {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn() {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    private void SceneLoaded(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut
    ) {
        if (IsHost && sceneName == "Game") {
            foreach (var id in clientsCompleted) {
                GameObject p = Instantiate(player, Vector3.zero, Quaternion.identity);
                p.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);
            }
        }
    }
}