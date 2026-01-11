using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;

public class PlayerSpawner : NetworkBehaviour {
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject lobbyEnjoyer;

    public static PlayerSpawner instance;
    public static event Action<ulong, Vector3> PlayerDiedServer; // Событие смерти на сервере (кто и где умер)

    private List<ulong> toKill = new();

    private void Start() {
        instance = this;
        DontDestroyOnLoad(gameObject);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void Update() {
        if (!IsServer) return;

        if (toKill.Count > 0) {
            var player = toKill[0];
            toKill.RemoveAt(0);
            StartCoroutine(HandleDeath(player));
        }
    }

    private void OnClientConnected(ulong clientId) {
        if (IsServer) {
            SpawnLobbyEnjoyer(clientId);
        }
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private IEnumerator HandleDeath(ulong clientId) {
        Debug.Log($"[PlayerSpawner] Сервер: Ждем перед тем как удалить игрока {clientId}");
        yield return new WaitForSeconds(5);
        if (DestroyClient(clientId)) {
            yield return new WaitForEndOfFrame();
            SpawnPlayer(clientId);
        }
    }

    private bool DestroyClient(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var playerObj = client.PlayerObject;
            if (playerObj != null && playerObj.TryGetComponent<Player>(out _)) {
                playerObj.Despawn(true);
                return true;
            }
        }

        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleDeathServerRpc(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var player = client.PlayerObject;
            if (player != null && player.IsSpawned) {
                Debug.Log($"[PlayerSpawner] Сервер: Игрок {clientId} умирает");

                var pos = player.transform.position;
                PlayerDiedServer?.Invoke(clientId, pos);

                HandleDeathClientRpc(clientId);
                if (!toKill.Contains(clientId))
                    toKill.Add(clientId);
                Debug.Log($"[PlayerSpawner] Сервер: Добавляем {clientId} в очередь на удаление");
            }
        }
    }

    [ClientRpc]
    private void HandleDeathClientRpc(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var player = client.PlayerObject;
            Debug.Log($"[PlayerSpawner] Клиент: Отключаем контроль над игроком {clientId}");
            player.GetComponentInChildren<MeshController>(true).SetRagdoll(true);
            player.GetComponentInChildren<Freeze>(true).gameObject.SetActive(false);
            player.GetComponent<CharacterController>().enabled = false;
            player.GetComponent<PlayerSpellCaster>().enabled = false;
            player.GetComponent<FirstPersonMovement>().enabled = false;
            player.GetComponent<FirstPersonLook>().enabled = false;
            player.GetComponent<Mouth>().enabled = false;
            if (NetworkManager.Singleton.LocalClientId == clientId) {
                player.GetComponentInChildren<Observer>(true).gameObject.SetActive(true);
                player.GetComponent<SteamVoiceChat>().DisableMicrophone();
            }
        }
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
        if (!IsServer) return;
        if (sceneName == GameProgress.Instance.SceneName) {
            foreach (var id in clientsCompleted) {
                SpawnPlayer(id);
            }
        }

        if (sceneName == "MainMenu") {
            foreach (var id in clientsCompleted) {
                SpawnLobbyEnjoyer(id);
            }
        }
    }

    private void SpawnPlayer(ulong clientId) {
        //TODO crash here NPE
        var team = TeamManager.Instance.GetTeam(clientId);
        var spawn = FindFirstObjectByType<Spawn>();
        if (spawn == null) {
            Debug.LogError($"[PlayerSpawner] Сервер: Ошибка! Не найден объект Spawn для спавна игрока {clientId} (Возможно сцена сменилась на не игровую)");
            return;
        }
        Debug.Log($"[PlayerSpawner] Сервер: Спавним игрока {clientId} в команде {team}");
        var spawnPoint = spawn.Get(team);
        var position = spawnPoint.position;
        var rotation = spawnPoint.rotation;

        GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
        newPlayer.transform.SetPositionAndRotation(position, rotation);
        var netObj = newPlayer.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        newPlayer.GetComponent<Player>().Init(clientId, position, rotation);
    }

    private void SpawnLobbyEnjoyer(ulong clientId) {
        var newLobbyEnjoyer = Instantiate(lobbyEnjoyer, Vector3.zero, Quaternion.identity);
        newLobbyEnjoyer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[Menu] Сервер: Создан новый LobbyEnjoyer_{clientId}");
    }
}