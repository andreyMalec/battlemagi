using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Steamworks;

public class PlayerManager : NetworkBehaviour {
    public static PlayerManager Instance;

    private Dictionary<ulong, SteamId> clientToSteam = new Dictionary<ulong, SteamId>();
    private Dictionary<SteamId, Transform> players = new Dictionary<SteamId, Transform>();

    private List<(SteamId, ulong)> pendingRegistrations = new List<(SteamId, ulong)>();

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlayerManager] Создан (Singleton, DontDestroyOnLoad)");
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void Update() {
        if (pendingRegistrations.Count > 0) {
            for (int i = pendingRegistrations.Count - 1; i >= 0; i--) {
                var (sid, clientId) = pendingRegistrations[i];
                if (TryAttachPlayer(sid, clientId)) {
                    Debug.Log($"[PlayerManager] Отложенная привязка выполнена для {sid} (clientId={clientId})");
                    pendingRegistrations.RemoveAt(i);
                }
            }
        }
    }

    private void OnClientConnected(ulong clientId) {
        Debug.Log($"[PlayerManager] Подключился clientId={clientId}");

        // Локальный клиент сообщает серверу свой SteamId
        if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsClient) {
            Debug.Log($"[PlayerManager] Локальный клиент отправляет серверу SteamId {SteamClient.SteamId}");
            RegisterSteamIdServerRpc((ulong)SteamClient.SteamId, clientId);
        }
    }

    private void OnClientDisconnected(ulong clientId) {
        if (clientToSteam.TryGetValue(clientId, out SteamId steamId)) {
            clientToSteam.Remove(clientId);

            if (IsServer)
                UnregisterPlayerClientRpc((ulong)steamId);

            Debug.Log($"[PlayerManager] Игрок {steamId} отключился (clientId={clientId})");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterSteamIdServerRpc(ulong steamId, ulong clientId) {
        SteamId sid = (SteamId)steamId;

        if (!clientToSteam.ContainsKey(clientId)) {
            clientToSteam.Add(clientId, sid);
            Debug.Log($"[PlayerManager] Сервер: зарегистрирован SteamId {sid} для clientId={clientId}");
        }

        // 📡 Разослать всем инфу про нового игрока
        RegisterPlayerClientRpc(steamId, clientId);

        // 📡 Отправить НОВОМУ игроку информацию про ВСЕХ существующих
        foreach (var kvp in clientToSteam) {
            if (kvp.Key == clientId) continue; // себя он уже знает
            SendExistingPlayerClientRpc((ulong)kvp.Value, kvp.Key, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }
    }

    [ClientRpc]
    private void RegisterPlayerClientRpc(ulong steamId, ulong clientId) {
        SteamId sid = (SteamId)steamId;
        if (!TryAttachPlayer(sid, clientId)) {
            Debug.LogWarning($"[PlayerManager] Не удалось сразу привязать {sid} (clientId={clientId}), в очередь");
            pendingRegistrations.Add((sid, clientId));
        }
    }

    [ClientRpc]
    private void SendExistingPlayerClientRpc(ulong steamId, ulong clientId, ClientRpcParams rpcParams = default) {
        SteamId sid = (SteamId)steamId;
        if (!TryAttachPlayer(sid, clientId)) {
            Debug.LogWarning(
                $"[PlayerManager] (Синхронизация) не удалось привязать {sid} (clientId={clientId}), в очередь");
            pendingRegistrations.Add((sid, clientId));
        } else {
            Debug.Log($"[PlayerManager] (Синхронизация) привязан {sid} (clientId={clientId}) для нового игрока");
        }
    }

    [ClientRpc]
    private void UnregisterPlayerClientRpc(ulong steamId) {
        if (players.Remove((SteamId)steamId)) {
            Debug.Log($"[PlayerManager] Клиент: удалён игрок {steamId}");
        }
    }

    private bool TryAttachPlayer(SteamId sid, ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var playerObj = client.PlayerObject;
            if (playerObj != null) {
                if (!players.ContainsKey(sid)) {
                    players.Add(sid, playerObj.transform);
                    Debug.Log($"[PlayerManager] Привязан {sid} → {playerObj.name}");
                }

                return true;
            }
        }

        return false;
    }

    public Transform GetPlayerTransform(SteamId steamId) {
        if (players.TryGetValue(steamId, out Transform t))
            return t;

        Debug.LogWarning($"[PlayerManager] Transform для {steamId} не найден");
        return null;
    }

    public SteamId? GetSteamIdByClientId(ulong clientId) {
        if (clientToSteam.TryGetValue(clientId, out SteamId sid))
            return sid;

        return null;
    }

    public IEnumerable<KeyValuePair<SteamId, Transform>> GetAllPlayers() {
        return players;
    }
}