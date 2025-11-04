using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;

public class PlayerSpawner : NetworkBehaviour {
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Shader playerBodyShader;
    [SerializeField] private Shader playerCloakShader;
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
        DestroyClientServerRpc(clientId);
    }

    [ServerRpc]
    private void DestroyClientServerRpc(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var playerObj = client.PlayerObject;
            playerObj.Despawn();
            if (playerObj != null)
                Destroy(playerObj.gameObject);

            SpawnPlayerServerRpc(clientId);
        }
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
        if (IsHost && sceneName == GameScene.Name) {
            foreach (var id in clientsCompleted) {
                SpawnPlayerServerRpc(id);
            }
        }
    }

    [ServerRpc]
    private void SpawnPlayerServerRpc(ulong clientId) {
        var team = TeamManager.Instance.GetTeam(clientId);
        var spawnPoint = FindFirstObjectByType<Spawn>().Get(team);
        var position = spawnPoint.position;
        var rotation = spawnPoint.rotation;

        GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
        newPlayer.transform.SetPositionAndRotation(position, rotation);

        var netObj = newPlayer.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        var movement = newPlayer.GetComponent<FirstPersonMovement>();
        movement.spawnPoint.Value = position;
        Debug.Log($"[PlayerSpawner] Сервер: Player_{clientId} создан в {position}, {rotation}");

        ApplyMaterialClientRpc(clientId, rotation);
    }

    [ClientRpc]
    private void ApplyMaterialClientRpc(ulong clientId, Quaternion rotation) {
        var steamid = PlayerManager.Instance.GetSteamId(clientId);
        if (!steamid.HasValue) return;
        var color = new Friend(steamid.Value).GetColor();
        var bodyMat = new Material(playerBodyShader);
        bodyMat.SetFloat(ColorizeMesh.Hue, color.hue);
        bodyMat.SetFloat(ColorizeMesh.Saturation, color.saturation);
        var cloakMat = new Material(playerCloakShader);
        cloakMat.SetFloat(ColorizeMesh.Hue, color.hue);
        cloakMat.SetFloat(ColorizeMesh.Saturation, color.saturation);
        var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        player.GetComponentInChildren<MeshBody>().gameObject.GetComponent<SkinnedMeshRenderer>().material = bodyMat;
        player.GetComponentInChildren<MeshCloak>().gameObject.GetComponent<SkinnedMeshRenderer>().material =
            cloakMat;

        player.GetComponent<FirstPersonLook>().ApplyInitialRotation(rotation);
    }

    private void SpawnLobbyEnjoyer(ulong clientId) {
        var newLobbyEnjoyer = Instantiate(lobbyEnjoyer, Vector3.zero, Quaternion.identity);
        newLobbyEnjoyer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[Menu] Сервер: Создан новый LobbyEnjoyer_{clientId}");
    }
}