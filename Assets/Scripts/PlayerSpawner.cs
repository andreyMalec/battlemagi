using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour {
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject lobbyEnjoyer;

    public static PlayerSpawner instance;

    private List<ulong> toKill = new();

    private void Start() {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update() {
        if (!IsServer) return;

        if (toKill.Count > 0) {
            var player = toKill[0];
            toKill.RemoveAt(0);
            StartCoroutine(HandleDeath(player));
        }
    }

    private IEnumerator HandleDeath(ulong clientId) {
        Debug.Log($"[PlayerSpawner] Сервер: Ждем перед тем как удалить игрока {clientId}");
        yield return new WaitForSeconds(5);
        DestroyClientServerRpc(clientId);
    }

    [ServerRpc]
    private void DestroyClientServerRpc(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var playerObj = client.PlayerObject;
            playerObj.Despawn();
            Destroy(playerObj.gameObject);

            SpawnPlayerServerRpc(clientId, Vector3.zero);
        }
    }

    [ServerRpc]
    public void HandleDeathServerRpc(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var player = client.PlayerObject;
            if (player != null && player.IsSpawned && player.OwnerClientId == clientId) {
                Debug.Log($"[PlayerSpawner] Сервер: Игрок {clientId} умирает");
                HandleDeathClientRpc(clientId);
                toKill.Add(clientId);
                Debug.Log($"[PlayerSpawner] Сервер: Добавляем {clientId} в очередь на удаление");
            }
        }
    }

    [ClientRpc]
    private void HandleDeathClientRpc(ulong clientId) {
        var steamId = PlayerManager.Instance.GetSteamIdByClientId(clientId);
        Debug.Log($"[PlayerSpawner] HandleDeathClientRpc {clientId} steamId={steamId}; {steamId.HasValue}");
        if (!steamId.HasValue) return;
        var player = PlayerManager.Instance.GetPlayerTransform(steamId.Value).gameObject;
        Debug.Log($"[PlayerSpawner] Клиент: Отключаем контроль над игроком {clientId}");
        player.GetComponentInChildren<MeshController>().SetRagdoll(true);
        player.GetComponent<CharacterController>().enabled = false;
        player.GetComponent<FirstPersonMovement>().enabled = false;
        player.GetComponent<FirstPersonLook>().enabled = false;
        if (NetworkManager.Singleton.LocalClientId == clientId)
            player.GetComponentInChildren<Observer>(true).gameObject.SetActive(true);
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
            var i = 0;
            foreach (var id in clientsCompleted) {
                i++;
                SpawnPlayerServerRpc(id, new Vector3(i * 10, 3, i * 10));
            }
        }
    }

    [ServerRpc]
    private void SpawnPlayerServerRpc(ulong clientId, Vector3 position) {
        GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
        var name = $"Player_{clientId}";
        newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        SetNameClientRpc(clientId, name);
        PlayerManager.Instance.ResetPlayerTransform(clientId, newPlayer);
        Debug.Log($"[PlayerSpawner] Сервер: Создан новый {name}");
    }

    [ClientRpc]
    private void SetNameClientRpc(ulong clientId, string name) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            client.PlayerObject.name = name;
        }
    }

    public void SpawnLobbyEnjoyer(ulong clientId) {
        var p = Instantiate(lobbyEnjoyer, Vector3.zero, Quaternion.identity);
        var name = $"LobbyEnjoyer_{clientId}";
        p.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        SetNameClientRpc(clientId, name);
        Debug.Log($"[Menu] Сервер: Создан новый {name}");
    }
}