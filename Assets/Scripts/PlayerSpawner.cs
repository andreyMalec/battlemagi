using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour {
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private GameObject lobbyEnjoyer;

    public static PlayerSpawner instance;
    public static event Action<ulong, Vector3> PlayerDiedServer; // Событие смерти на сервере (кто и где умер)

    private List<ulong> toKill = new();

    public struct ParticipantSpawnDescriptor {
        public ParticipantId ParticipantId;
        public ulong SteamId;
        public int Archetype;
        public float Hue;
        public float Saturation;
        public TeamManager.Team Team;
    }

    private void Start() {
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton != null)
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

    private bool IsMatchInProgress() {
        return IsServer && LobbyManager.Instance != null &&
               LobbyManager.Instance.State == LobbyManager.PlayerState.InGame;
    }

    private void OnClientConnected(ulong clientId) {
        if (IsServer) {
            if (IsMatchInProgress())
                StartCoroutine(RespawnPlayer(clientId));
            else
                SpawnLobbyEnjoyer(clientId);
        }
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private IEnumerator RespawnPlayer(ulong clientId) {
        const float timeout = 10f;
        var elapsed = 0f;

        while (!IsPlayerSpawnDataReady(clientId)) {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                yield break;

            if (elapsed >= timeout) {
                Debug.LogError(
                    $"[PlayerSpawner] Сервер: Не дождались синхронизации PlayerManager/TeamManager для игрока {clientId}");
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SpawnPlayer(clientId);
    }

    private bool IsPlayerSpawnDataReady(ulong clientId) {
        return PlayerManager.Instance != null &&
               TeamManager.Instance != null &&
               PlayerManager.Instance.TryGetPlayerData(clientId, out _) &&
               TeamManager.Instance.HasTeam(clientId);
    }

    private IEnumerator HandleDeath(ulong clientId) {
        Debug.Log($"[PlayerSpawner] Сервер: Ждем перед тем как удалить игрока {clientId}");
        yield return new WaitForSeconds(5);
        DestroyClient(clientId);
        yield return new WaitForEndOfFrame();
        SpawnPlayer(clientId);
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

    public void HandleDeathServer(ulong clientId) {
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
            player.GetComponent<SpellCasterPlayer>().enabled = false;
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
        if (NetworkManager.Singleton != null)
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
                StartCoroutine(RespawnPlayer(id));
            }
        }

        if (sceneName == "MainMenu") {
            foreach (var id in clientsCompleted) {
                SpawnLobbyEnjoyer(id);
            }
        }
    }

    private void SpawnPlayer(ulong clientId) {
        if (!PlayerManager.Instance.TryGetPlayerData(clientId, out var playerData)) {
            Debug.LogError($"[PlayerSpawner] Сервер: Не найдены данные PlayerManager для игрока {clientId}");
            return;
        }

        var descriptor = new ParticipantSpawnDescriptor {
            ParticipantId = ParticipantId.Human(clientId),
            SteamId = playerData.SteamId,
            Archetype = playerData.Archetype,
            Hue = playerData.Hue,
            Saturation = playerData.Saturation,
            Team = TeamManager.Instance.GetTeam(clientId)
        };

        SpawnParticipant(descriptor, clientId);
    }

    public GameObject SpawnBotObject(ulong botId) {
        if (!PlayerManager.Instance.TryGetBotData(botId, out var botData)) {
            Debug.LogError($"[PlayerSpawner] Bot {botId} is not registered in PlayerManager");
            return null;
        }

        var descriptor = new ParticipantSpawnDescriptor {
            ParticipantId = botData.Id,
            SteamId = botData.SteamId,
            Archetype = botData.Archetype,
            Hue = botData.Hue,
            Saturation = botData.Saturation,
            Team = TeamManager.Instance.GetTeam(botData.Id)
        };

        return SpawnParticipant(descriptor, NetworkManager.ServerClientId);
    }

    private GameObject SpawnParticipant(
        ParticipantSpawnDescriptor descriptor,
        ulong ownerClientId
    ) {
        var spawn = FindFirstObjectByType<Spawn>();
        if (spawn == null) {
            Debug.LogError(
                $"[PlayerSpawner] Сервер: Ошибка! Не найден объект Spawn для спавна участника {descriptor.ParticipantId} (Возможно сцена сменилась на не игровую)");
            return null;
        }

        Debug.Log($"[PlayerSpawner] Сервер: Спавним участника {descriptor.ParticipantId} в команде {descriptor.Team}");
        var spawnPoint = spawn.Get(descriptor.Team);
        var position = spawnPoint.position;
        var rotation = spawnPoint.rotation;

        if (descriptor.ParticipantId.IsBot) {
            GameObject newBot = Instantiate(botPrefab, position, rotation);
            newBot.transform.SetPositionAndRotation(position, rotation);
            var botIdentity = newBot.GetComponent<ParticipantIdentity>();
            botIdentity.SetParticipantId(descriptor.ParticipantId);
            foreach (var identityUser in newBot.GetComponents<IdentityUser>()) {
                identityUser.Use(newBot);
            }

            var bot = newBot.GetComponent<Bot>();
            bot.ApplyPlayerState(descriptor.SteamId, descriptor.Archetype, descriptor.Hue, descriptor.Saturation);

            var netObj = newBot.GetComponent<NetworkObject>();
            netObj.Spawn(true);

            newBot.name = $"Bot_{descriptor.ParticipantId.Value}";

            return newBot;
        } else {
            GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
            newPlayer.transform.SetPositionAndRotation(position, rotation);
            var participantIdentity = newPlayer.GetComponent<ParticipantIdentity>();
            participantIdentity.SetParticipantId(descriptor.ParticipantId);
            foreach (var identityUser in newPlayer.GetComponents<IdentityUser>()) {
                identityUser.Use(newPlayer);
            }

            var player = newPlayer.GetComponent<Player>();
            player.ApplyPlayerState(descriptor.SteamId, descriptor.Archetype, descriptor.Hue, descriptor.Saturation);

            var netObj = newPlayer.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(ownerClientId, true);
            player.Init(ParticipantIdentityCodec.Encode(participantIdentity.Id), position, rotation);

            newPlayer.name = $"Player_{ownerClientId}";

            return newPlayer;
        }
    }

    private void SpawnLobbyEnjoyer(ulong clientId) {
        var newLobbyEnjoyer = Instantiate(lobbyEnjoyer, Vector3.zero, Quaternion.identity);
        newLobbyEnjoyer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[PlayerSpawner] Сервер: Создан новый LobbyEnjoyer_{clientId}");
    }
}