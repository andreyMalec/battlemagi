using System;
using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour {
    [Serializable]
    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData> {
        public ulong ClientId;
        public ulong SteamId;
        public int Kills;
        public int Deaths;
        public int Assists;
        public int Flags;
        public int Archetype;
        public float Hue;
        public float Saturation;

        public PlayerData(ulong clientId, ulong steamId) {
            ClientId = clientId;
            SteamId = steamId;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            Flags = 0;
            Archetype = 0;
            Hue = 78f;
            Saturation = 0.5f;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref SteamId);
            serializer.SerializeValue(ref Kills);
            serializer.SerializeValue(ref Deaths);
            serializer.SerializeValue(ref Assists);
            serializer.SerializeValue(ref Flags);
            serializer.SerializeValue(ref Archetype);
            serializer.SerializeValue(ref Hue);
            serializer.SerializeValue(ref Saturation);
        }

        public bool Equals(PlayerData other) =>
            ClientId == other.ClientId && SteamId == other.SteamId && Kills == other.Kills && Deaths == other.Deaths &&
            Assists == other.Assists && Flags == other.Flags && Archetype == other.Archetype &&
            Mathf.Approximately(Hue, other.Hue) && Mathf.Approximately(Saturation, other.Saturation);

        public override string ToString() {
            return $"PlayerData({ClientId}, {SteamId}, {Archetype}, {Flags}, {Kills}, {Deaths}, {Assists}, {Hue}, {Saturation})";
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
        NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }
    }

    private void OnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2) {
        if (arg2.EventType == ConnectionEvent.ClientDisconnected) {
            OnClientDisconnected(arg2.ClientId);
        }

        if (arg2.EventType == ConnectionEvent.ClientConnected) {
            OnClientConnected(arg2.ClientId);
        }
    }

    private void OnClientConnected(ulong clientId) {
        if (clientId == NetworkManager.Singleton.LocalClientId) {
            ulong steamId = SteamClient.SteamId;
            RegisterPlayerServerRpc(steamId);
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

    private bool IsMatchInProgress() {
        return IsServer && LobbyManager.Instance != null && LobbyManager.Instance.State == LobbyManager.PlayerState.InGame;
    }

    private int FindIndexBySteamId(ulong steamId) {
        for (int i = 0; i < players.Count; i++) {
            if (players[i].SteamId == steamId)
                return i;
        }

        return -1;
    }

    private void SyncExistingPlayersToClient(ulong clientId) {
        foreach (var member in players) {
            if (member.ClientId == clientId) continue;
            RegisterPlayerClientRpc(member.SteamId, member.ClientId, new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(ulong steamId, ServerRpcParams rpcParams = default) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int existingIndex = FindIndexBySteamId(steamId);
        if (existingIndex >= 0) {
            var existing = players[existingIndex];
            if (existing.ClientId != clientId) {
                var previousClientId = existing.ClientId;
                existing.ClientId = clientId;
                players[existingIndex] = existing;
                if (IsMatchInProgress())
                    TeamManager.Instance.ReplaceClientId(previousClientId, clientId);
                Debug.Log($"[Server] Reconnected player SteamId={steamId}: ClientId {previousClientId} -> {clientId}");
            }

            SyncExistingPlayersToClient(clientId);
            return;
        }

        if (IsMatchInProgress()) {
            Debug.LogWarning($"[Server] Rejecting mid-game join ClientId={clientId}, SteamId={steamId}");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        PlayerData data = new PlayerData(clientId, steamId);
        players.Add(data);
        OnPlayerAdded?.Invoke(data);
        SyncExistingPlayersToClient(clientId);

        Debug.Log($"[Server] Registered player ClientId={clientId}, SteamId={steamId}");
    }

    [ClientRpc]
    private void RegisterPlayerClientRpc(ulong steamId, ulong clientId, ClientRpcParams rpcParams = default) {
        PlayerData data = new PlayerData(clientId, steamId);
        OnPlayerAdded?.Invoke(data);
        Debug.Log($"[PlayerManager] Клиент: Зарегистрирован SteamId {steamId} для clientId={clientId}");
    }

    private void OnClientDisconnected(ulong clientId) {
        if (!IsServer && (clientId == 0 || clientId == NetworkManager.Singleton.LocalClientId)) {
            LobbyManager.Instance.LeaveLobby();
            SceneManager.LoadScene("MainMenu");
            return;
        }

        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                if (IsServer) {
                    if (IsMatchInProgress()) {
                        Debug.Log($"[Server] Preserving player ClientId={clientId} for reconnect");
                    } else {
                        Debug.Log($"[Server] Removing player ClientId={clientId}");
                        players.RemoveAt(i);
                    }
                }

                OnPlayerRemoved?.Invoke(player);
                break;
            }
        }
    }

    public SteamId? GetSteamId(ulong clientId) {
        return FindByClientId(clientId)?.SteamId;
    }

    public PlayerColor? GetColor(ulong steamId) {
        var player = FindBySteamId(steamId);
        if (!player.HasValue)
            return null;

        return new PlayerColor(player.Value.Hue, player.Value.Saturation);
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

    public void AddFlag(ulong clientId) {
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddFlag for Player_{clientId}");
                player.Flags++;
                players[i] = player;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetArchetypeServerRpc(int archetype, ServerRpcParams rpcParams = default) {
        var clientId = rpcParams.Receive.SenderClientId;
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"SetArchetype for Player_{clientId} to {archetype}");
                player.Archetype = archetype;
                players[i] = player;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(float hue, float saturation, ServerRpcParams rpcParams = default) {
        var clientId = rpcParams.Receive.SenderClientId;
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                player.Hue = hue;
                player.Saturation = saturation;
                players[i] = player;
            }
        }
    }
}