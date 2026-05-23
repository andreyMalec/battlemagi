using System;
using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour, IParticipantRegistry {
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
        public int PingMs;
        public float PacketLossPercent;

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
            PingMs = 0;
            PacketLossPercent = 0f;
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
            serializer.SerializeValue(ref PingMs);
            serializer.SerializeValue(ref PacketLossPercent);
        }

        public bool Equals(PlayerData other) =>
            ClientId == other.ClientId && SteamId == other.SteamId && Kills == other.Kills && Deaths == other.Deaths &&
            Assists == other.Assists && Flags == other.Flags && Archetype == other.Archetype &&
            Mathf.Approximately(Hue, other.Hue) && Mathf.Approximately(Saturation, other.Saturation) &&
            PingMs == other.PingMs && Mathf.Approximately(PacketLossPercent, other.PacketLossPercent);

        public override string ToString() {
            return $"PlayerData({ClientId}, {SteamId}, {Archetype}, {Flags}, {Kills}, {Deaths}, {Assists}, {Hue}, {Saturation}, {PingMs}, {PacketLossPercent})";
        }

        public GameObject PlayerObject() {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ClientId, out var client))
                return client.PlayerObject.gameObject;
            return null;
        }
    }

    public event Action<PlayerData> OnPlayerAdded;
    public event Action<PlayerData> OnPlayerRemoved;
    public event Action<NetworkList<MatchParticipantData>> OnListChanged;
    public event Action<IReadOnlyList<MatchParticipantData>> OnParticipantsChanged;

    private NetworkList<PlayerData> players;
    private NetworkList<MatchParticipantData> participants;
    private readonly Dictionary<ulong, MatchParticipantData> _botParticipants = new();
    private readonly List<MatchParticipantData> _participantsBuffer = new();

    [SerializeField] private List<PlayerData> debugPlayers = new(); // видимый в инспекторе
    [SerializeField] private List<MatchParticipantData> debugBots = new();
    [SerializeField] private float networkStatsRefreshInterval = 1f;

    public static PlayerManager Instance { get; private set; }

    private float _networkStatsRefreshTimer;
    private int _lastReportedPingMs = -1;
    private float _lastReportedPacketLossPercent = -1f;

    public IReadOnlyList<MatchParticipantData> Participants {
        get {
            _participantsBuffer.Clear();
            foreach (var participant in participants)
                _participantsBuffer.Add(participant);
            return _participantsBuffer;
        }
    }

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
        players = new NetworkList<PlayerData>();
        participants = new NetworkList<MatchParticipantData>();
        players.OnListChanged += OnPlayersChanged;
        participants.OnListChanged += OnParticipantsListChanged;
    }

    private void Start() {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
    }

    private void Update() {
        if (!IsSpawned) return;

        _networkStatsRefreshTimer -= Time.unscaledDeltaTime;
        if (_networkStatsRefreshTimer > 0f) return;

        _networkStatsRefreshTimer = networkStatsRefreshInterval;

        if (IsClient)
            ReportLocalNetworkStats();

        if (IsServer && !IsClient)
            SyncNetworkStats();
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (players != null)
            players.OnListChanged -= OnPlayersChanged;
        if (participants != null)
            participants.OnListChanged -= OnParticipantsListChanged;

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
        if (IsServer) {
            players.Clear();
            participants.Clear();
            _botParticipants.Clear();
            _networkStatsRefreshTimer = 0f;
        }
    }

    private Netcode.Transports.Facepunch.FacepunchTransport GetFacepunchTransport() {
        return NetworkManager.Singleton.NetworkConfig.NetworkTransport as Netcode.Transports.Facepunch.FacepunchTransport;
    }

    private void SyncNetworkStats() {
        var transport = GetFacepunchTransport();
        if (transport == null) return;

        for (int i = 0; i < players.Count; i++) {
            var player = players[i];
            int pingMs = 0;
            float packetLossPercent = 0f;

            if (player.ClientId != NetworkManager.ServerClientId)
                transport.TryGetNetworkMetrics(player.ClientId, out pingMs, out packetLossPercent);

            if (player.PingMs == pingMs && Mathf.Approximately(player.PacketLossPercent, packetLossPercent)) continue;

            player.PingMs = pingMs;
            player.PacketLossPercent = packetLossPercent;
            players[i] = player;
        }
    }

    private void ReportLocalNetworkStats() {
        var transport = GetFacepunchTransport();
        if (transport == null) return;
        if (!transport.TryGetNetworkMetrics(NetworkManager.ServerClientId, out var pingMs, out var packetLossPercent)) return;

        if (_lastReportedPingMs == pingMs && Mathf.Approximately(_lastReportedPacketLossPercent, packetLossPercent)) return;

        _lastReportedPingMs = pingMs;
        _lastReportedPacketLossPercent = packetLossPercent;
        UpdateNetworkStatsServerRpc(pingMs, packetLossPercent);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateNetworkStatsServerRpc(int pingMs, float packetLossPercent, ServerRpcParams rpcParams = default) {
        var clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < players.Count; i++) {
            var player = players[i];
            if (player.ClientId != clientId) continue;
            if (player.PingMs == pingMs && Mathf.Approximately(player.PacketLossPercent, packetLossPercent)) return;

            player.PingMs = pingMs;
            player.PacketLossPercent = packetLossPercent;
            players[i] = player;
            return;
        }
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerData> changeEvent) {
        debugPlayers.Clear();
        foreach (var player in players) {
            debugPlayers.Add(player);
            if (IsServer)
                UpsertParticipant(ToMatchParticipantData(player));
        }

        if (!IsServer)
            return;

        for (int i = participants.Count - 1; i >= 0; i--) {
            var participant = participants[i];
            if (!participant.Id.IsHuman)
                continue;

            if (FindByClientId(participant.Id.Value).HasValue)
                continue;

            participants.RemoveAt(i);
        }
    }

    private void OnParticipantsListChanged(NetworkListEvent<MatchParticipantData> changeEvent) {
        _ = changeEvent;
        OnListChanged?.Invoke(participants);
        NotifyParticipantsChanged();
        RefreshDebugBots();
    }

    private void NotifyParticipantsChanged() {
        _participantsBuffer.Clear();
        foreach (var participant in participants)
            _participantsBuffer.Add(participant);
        OnParticipantsChanged?.Invoke(_participantsBuffer);
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
            RegisterPlayerClientRpc(member, new ClientRpcParams {
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
    private void RegisterPlayerClientRpc(PlayerData data, ClientRpcParams rpcParams = default) {
        OnPlayerAdded?.Invoke(data);
        Debug.Log($"[PlayerManager] Клиент: Зарегистрирован SteamId {data.SteamId} для clientId={data.ClientId}");
    }

    private void OnClientDisconnected(ulong clientId) {
        if (!IsServer && (clientId == 0 || clientId == NetworkManager.Singleton.LocalClientId)) {
            LobbyManager.Instance.LeaveLobby();
            
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

    public bool TryGetPlayerData(ulong clientId, out PlayerData data) {
        var player = FindByClientId(clientId);
        if (player.HasValue) {
            data = player.Value;
            return true;
        }

        data = default;
        return false;
    }

    public bool TryGetParticipant(ParticipantId participantId, out MatchParticipantData data) {
        for (var i = 0; i < participants.Count; i++) {
            var participant = participants[i];
            if (participant.Id != participantId)
                continue;
            data = participant;
            return true;
        }

        data = default;
        return false;
    }

    public bool TryGetParticipantBySteamId(ulong steamId, out MatchParticipantData data) {
        for (var i = 0; i < participants.Count; i++) {
            var participant = participants[i];
            if (participant.SteamId != steamId) continue;
            data = participant;
            return true;
        }

        data = default;
        return false;
    }

    public void RegisterParticipant(MatchParticipantData data) {
        if (data.Id.IsHuman) {
            Debug.LogWarning($"[PlayerManager] RegisterParticipant ignored for human {data.Id}. Humans are synced by netcode.");
            return;
        }

        _botParticipants[data.Id.Value] = data;
        if (IsServer)
            UpsertParticipant(data);
    }

    public bool RemoveParticipant(ParticipantId participantId) {
        if (!participantId.IsBot) return false;
        var removed = _botParticipants.Remove(participantId.Value);
        if (removed && IsServer)
            RemoveParticipantEntry(participantId);
        return removed;
    }

    public void RegisterBot(ulong botId, ulong steamId = 0) {
        var id = ParticipantId.Bot(botId);
        var data = new MatchParticipantData(id, steamId);
        RegisterParticipant(data);
    }

    public bool TryGetBotData(ulong botId, out MatchParticipantData data) {
        return TryGetParticipant(ParticipantId.Bot(botId), out data);
    }

    public bool TryGetPlayerDataBySteamId(ulong steamId, out PlayerData data) {
        var player = FindBySteamId(steamId);
        if (player.HasValue) {
            data = player.Value;
            return true;
        }

        data = default;
        return false;
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
        var participantId = ParticipantIdentityCodec.Decode(clientId);
        if (participantId.IsBot) {
            if (_botParticipants.TryGetValue(participantId.Value, out var botData)) {
                botData.Kills = 0;
                botData.Deaths = 0;
                botData.Assists = 0;
                _botParticipants[participantId.Value] = botData;
                UpsertParticipant(botData);
            }

            return;
        }

        clientId = participantId.Value;
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
        var participantId = ParticipantIdentityCodec.Decode(clientId);
        if (participantId.IsBot) {
            AddKill(participantId);
            return;
        }

        clientId = participantId.Value;
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddKill for Player_{clientId}");
                player.Kills++;
                players[i] = player;
            }
        }
    }

    public void AddKill(ParticipantId participantId) {
        if (participantId.IsHuman) {
            AddKill(participantId.Value);
            return;
        }

        if (!_botParticipants.TryGetValue(participantId.Value, out var data)) return;
        data.Kills++;
        _botParticipants[participantId.Value] = data;
        UpsertParticipant(data);
    }

    public void AddDeath(ulong clientId) {
        var participantId = ParticipantIdentityCodec.Decode(clientId);
        if (participantId.IsBot) {
            AddDeath(participantId);
            return;
        }

        clientId = participantId.Value;
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddDeath for Player_{clientId}");
                player.Deaths++;
                players[i] = player;
            }
        }
    }

    public void AddDeath(ParticipantId participantId) {
        if (participantId.IsHuman) {
            AddDeath(participantId.Value);
            return;
        }

        if (!_botParticipants.TryGetValue(participantId.Value, out var data)) return;
        data.Deaths++;
        _botParticipants[participantId.Value] = data;
        UpsertParticipant(data);
    }

    public void AddAssist(ulong clientId) {
        var participantId = ParticipantIdentityCodec.Decode(clientId);
        if (participantId.IsBot) {
            AddAssist(participantId);
            return;
        }

        clientId = participantId.Value;
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddAssist for Player_{clientId}");
                player.Assists++;
                players[i] = player;
            }
        }
    }

    public void AddAssist(ParticipantId participantId) {
        if (participantId.IsHuman) {
            AddAssist(participantId.Value);
            return;
        }

        if (!_botParticipants.TryGetValue(participantId.Value, out var data)) return;
        data.Assists++;
        _botParticipants[participantId.Value] = data;
        UpsertParticipant(data);
    }

    public void AddFlag(ulong clientId) {
        var participantId = ParticipantIdentityCodec.Decode(clientId);
        if (participantId.IsBot) {
            AddFlag(participantId);
            return;
        }

        clientId = participantId.Value;
        for (int i = players.Count - 1; i >= 0; i--) {
            var player = players[i];
            if (player.ClientId == clientId) {
                Debug.Log($"AddFlag for Player_{clientId}");
                player.Flags++;
                players[i] = player;
            }
        }
    }

    public void AddFlag(ParticipantId participantId) {
        if (participantId.IsHuman) {
            AddFlag(participantId.Value);
            return;
        }

        if (!_botParticipants.TryGetValue(participantId.Value, out var data)) return;
        data.Flags++;
        _botParticipants[participantId.Value] = data;
        UpsertParticipant(data);
    }

    private int FindParticipantIndex(ParticipantId participantId) {
        for (var i = 0; i < participants.Count; i++) {
            if (participants[i].Id == participantId)
                return i;
        }

        return -1;
    }

    private void UpsertParticipant(MatchParticipantData data) {
        var index = FindParticipantIndex(data.Id);
        if (index >= 0) {
            participants[index] = data;
            return;
        }

        participants.Add(data);
    }

    private void RemoveParticipantEntry(ParticipantId participantId) {
        var index = FindParticipantIndex(participantId);
        if (index >= 0)
            participants.RemoveAt(index);
    }

    private static MatchParticipantData ToMatchParticipantData(PlayerData playerData) {
        return new MatchParticipantData(ParticipantId.Human(playerData.ClientId), playerData.SteamId) {
            Kills = playerData.Kills,
            Deaths = playerData.Deaths,
            Assists = playerData.Assists,
            Flags = playerData.Flags,
            Archetype = playerData.Archetype,
            Hue = playerData.Hue,
            Saturation = playerData.Saturation
        };
    }

    private void RefreshDebugBots() {
        debugBots.Clear();
        foreach (var participant in participants) {
            if (!participant.Id.IsBot)
                continue;
            debugBots.Add(participant);
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