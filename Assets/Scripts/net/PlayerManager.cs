using System;
using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour {
    [Serializable]
    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData> {
        public ulong ClientId;
        public ulong SteamId;
        public int Kills;
        public int Deaths;
        public int Assists;

        public PlayerData(ulong clientId, ulong steamId) {
            ClientId = clientId;
            SteamId = steamId;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref SteamId);
            serializer.SerializeValue(ref Kills);
            serializer.SerializeValue(ref Deaths);
            serializer.SerializeValue(ref Assists);
        }

        public bool Equals(PlayerData other) =>
            ClientId == other.ClientId && SteamId == other.SteamId && Kills == other.Kills && Deaths == other.Deaths &&
            Assists == other.Assists;

        public override string ToString() {
            return $"PlayerData({ClientId}, {SteamId}, {Kills}, {Deaths}, {Assists})";
        }

        public GameObject PlayerObject() {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ClientId, out var client))
                return client.PlayerObject.gameObject;
            return null;
        }
    }

    public event Action<PlayerData> OnPlayerAdded;
    public event Action<PlayerData> OnPlayerRemoved;
    public event Action<List<PlayerData>> OnListChanged;

    private NetworkList<PlayerData> players;

    [SerializeField] private List<PlayerData> debugPlayers = new(); // видимый в инспекторе

    public static PlayerManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
        players = new NetworkList<PlayerData>();
        players.OnListChanged += OnPlayersChanged;
    }

    private void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            ulong steamId = (ulong)SteamClient.SteamId;
            RegisterPlayerServerRpc(steamId);
        }
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public override void OnNetworkSpawn() {
        if (IsServer)
            players.Clear();
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerData> changeEvent) {
        debugPlayers.Clear();
        foreach (var player in players) {
            debugPlayers.Add(player);
        }
        OnListChanged?.Invoke(debugPlayers);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(ulong steamId, ServerRpcParams rpcParams = default) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        PlayerData data = new PlayerData(clientId, steamId);

        if (!players.Contains(data)) {
            players.Add(data);
            OnPlayerAdded?.Invoke(data);

            // Отправить НОВОМУ клиенту всех УЖЕ ПОДКЛЮЧЕННЫХ
            foreach (var member in players) {
                if (member.ClientId == clientId) continue;
                RegisterPlayerClientRpc(member.SteamId, member.ClientId, new ClientRpcParams {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }

        Debug.Log($"[Server] Registered player ClientId={clientId}, SteamId={steamId}");
    }

    [ClientRpc]
    private void RegisterPlayerClientRpc(ulong steamId, ulong clientId, ClientRpcParams rpcParams = default) {
        PlayerData data = new PlayerData(clientId, steamId);
        OnPlayerAdded?.Invoke(data);
        Debug.Log($"[PlayerManager] Клиент: Зарегистрирован SteamId {steamId} для clientId={clientId}");
    }

    private void OnClientDisconnected(ulong clientId) {
        if (!IsServer) return;

        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"[Server] Removing player ClientId={clientId}");
                players.RemoveAt(i);
                OnPlayerRemoved?.Invoke(player);
            }
        }
    }

    public SteamId? GetSteamId(ulong clientId) {
        return FindByClientId(clientId)?.SteamId;
    }

    public PlayerData? FindByClientId(ulong clientId) {
        foreach (var player in players) {
            if (player.ClientId == clientId) {
                return player;
            }
        }

        return null;
    }

    public PlayerData? FindBySteamId(ulong steamId) {
        foreach (var player in players) {
            if (player.SteamId == steamId) {
                return player;
            }
        }

        return null;
    }

    public List<PlayerData> Players() {
        var list = new List<PlayerData>();
        foreach (var player in players) {
            list.Add(player);
        }

        return list;
    }

    public void ResetScore(ulong clientId) {
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"ResetScore for Player_{clientId}");
                player.Kills = 0;
                player.Deaths = 0;
                player.Assists = 0;
                players[i] = player;
            }
        }
    }

    public void AddKill(ulong clientId) {
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddKill for Player_{clientId}");
                player.Kills++;
                players[i] = player;
            }
        }
    }

    public void AddDeath(ulong clientId) {
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddDeath for Player_{clientId}");
                player.Deaths++;
                players[i] = player;
            }
        }
    }

    public void AddAssist(ulong clientId) {
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddAssist for Player_{clientId}");
                player.Assists++;
                players[i] = player;
            }
        }
    }
}