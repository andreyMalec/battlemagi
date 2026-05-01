using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour {
    public enum TeamMode {
        FreeForAll,
        TwoTeams,
        CaptureTheFlag
    }

    public enum Team {
        None = -1,
        Red = 0,
        Blue = 1,
    }

    [Serializable]
    public struct TeamEntry : INetworkSerializable, IEquatable<TeamEntry> {
        public ulong clientId;
        public Team team;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref team);
        }

        public bool Equals(TeamEntry other) => clientId == other.clientId && team == other.team;
        public override int GetHashCode() => ((int)clientId * 397) ^ (int)team;
    }

    public event Action<Team> MyTeam;
    public event Action<int, int> OnScoreChanged;

    // ========== Sync data ==========
    public NetworkVariable<TeamMode> CurrentMode = new(TeamMode.FreeForAll);
    public NetworkVariable<int> RedScore = new(0);
    public NetworkVariable<int> BlueScore = new(0);
    public NetworkVariable<int> EndChoice = new(0);
    private NetworkList<TeamEntry> _teams;
    private readonly Dictionary<ulong, Team> _botTeams = new();

    public static TeamManager Instance { get; private set; }

    public bool isTeamMode => CurrentMode.Value != TeamMode.FreeForAll;

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
        _teams = new NetworkList<TeamEntry>();
        _teams.OnListChanged += OnTeamListChanged;

        RedScore.OnValueChanged += (_, newVal) => OnScoreChanged?.Invoke(newVal, BlueScore.Value);
        BlueScore.OnValueChanged += (_, newVal) => OnScoreChanged?.Invoke(RedScore.Value, newVal);
    }

    public void SetEndChoice(int choice) {
        if (!IsServer) return;
        EndChoice.Value = choice;
    }

    public void Reset() {
        if (!IsServer) return;
        CurrentMode.Value = TeamMode.FreeForAll;
        RedScore.Value = 0;
        BlueScore.Value = 0;
        EndChoice.Value = 0;
        _botTeams.Clear();
        RedistributePlayers();
    }

    private void Start() {
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        }
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }
    }

    private void OnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2) {
        if (!IsServer) return;

        if (arg2.EventType == ConnectionEvent.ClientDisconnected) {
            OnClientDisconnected(arg2.ClientId);
        }

        if (arg2.EventType == ConnectionEvent.ClientConnected) {
            OnClientConnected(arg2.ClientId);
        }
    }

    private bool IsMatchInProgress() {
        return IsServer && LobbyManager.Instance != null && LobbyManager.Instance.State == LobbyManager.PlayerState.InGame;
    }

    private int FindIndexByClientId(ulong clientId) {
        return _teams.ToList().FindIndex(e => e.clientId == clientId);
    }

    // ===============================
    // SERVER: Подключение/Отключение
    // ===============================
    private void OnClientConnected(ulong clientId) {
        if (!IsServer) return;
        if (IsMatchInProgress()) return;

        Team team = AssignTeam(clientId);
        _teams.Add(new TeamEntry { clientId = clientId, team = team });

        Debug.Log($"[TeamManager] {clientId} joined team {team}");
    }

    private void OnClientDisconnected(ulong clientId) {
        if (!IsServer) return;
        if (IsMatchInProgress()) return;

        int index = FindIndexByClientId(clientId);
        if (index >= 0) _teams.RemoveAt(index);
    }

    private Team AssignTeam(ulong clientId) {
        return AssignTeamInternal(clientId);
    }

    private Team AssignTeamInternal(ulong uniqueId) {
        if (!isTeamMode)
            return (Team)uniqueId; // уникальный ID — каждый сам за себя

        // TwoTeams — добавляем в менее заполненную
        int red = 0;
        int blue = 0;
        foreach (var entry in _teams) {
            if (entry.team == Team.Red)
                red++;
            if (entry.team == Team.Blue)
                blue++;
        }

        foreach (var team in _botTeams.Values) {
            if (team == Team.Red)
                red++;
            if (team == Team.Blue)
                blue++;
        }

        return red < blue ? Team.Red : Team.Blue;
    }

    public void SetMode(TeamMode mode) {
        if (!IsServer) return;
        CurrentMode.Value = mode;
        RedScore.Value = 0;
        BlueScore.Value = 0;
        RedistributePlayers();
    }

    public void ReplaceClientId(ulong previousClientId, ulong newClientId) {
        if (!IsServer) return;

        int index = FindIndexByClientId(previousClientId);
        if (index < 0) return;

        var entry = _teams[index];
        entry.clientId = newClientId;
        _teams[index] = entry;

        Debug.Log($"[TeamManager] Restored player team {entry.team} for {previousClientId} -> {newClientId}");
    }

    private void RedistributePlayers() {
        foreach (var entry in _teams) {
            Team newTeam = AssignTeam(entry.clientId);

            int idx = FindIndexByClientId(entry.clientId);
            if (idx >= 0)
                _teams[idx] = new TeamEntry { clientId = entry.clientId, team = newTeam };
        }

        if (isTeamMode) {
            var botIds = new List<ulong>(_botTeams.Keys);
            foreach (var botId in botIds) {
                _botTeams[botId] = AssignTeamInternal(botId);
            }
        }

        Debug.Log($"[TeamManager] Rebalanced into {CurrentMode.Value}");
    }

    public void RegisterBot(ulong botId, Team requestedTeam = Team.None) {
        if (!IsServer) return;

        var assignedTeam = requestedTeam;
        if (assignedTeam == Team.None || !isTeamMode)
            assignedTeam = AssignTeamInternal(botId);

        _botTeams[botId] = assignedTeam;
        Debug.Log($"[TeamManager] Bot {botId} joined team {assignedTeam}");
    }

    public void RemoveBot(ulong botId) {
        if (!IsServer) return;
        _botTeams.Remove(botId);
    }

    // ===============================
    // CLIENT: Обновление списка
    // ===============================
    private void OnTeamListChanged(NetworkListEvent<TeamEntry> e) {
        Debug.Log($"[TeamManager] Team list updated ({_teams.Count})");
        foreach (var entry in _teams) {
            if (entry.clientId == NetworkManager.LocalClientId)
                MyTeam?.Invoke(entry.team);
            Debug.Log($"   → Player {entry.clientId} : {entry.team}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestChangeTeamServerRpc(ulong clientId, int newTeamId) {
        if (!IsServer) return;
        int index = FindIndexByClientId(clientId);
        if (index < 0) return;

        var entry = _teams[index];
        if ((int)entry.team == newTeamId)
            return;
        if (isTeamMode && (newTeamId < 0 || newTeamId > 1))
            return;
        entry.team = (Team)newTeamId;
        _teams[index] = entry;

        Debug.Log($"[TeamManager] Player {clientId} switched to team {newTeamId}");
    }

    // ===============================
    // CTF Scoring
    // ===============================
    public void AddScore(Team team, int delta) {
        if (!IsServer) return;
        if (team == Team.Red) RedScore.Value += delta;
        if (team == Team.Blue) BlueScore.Value += delta;
    }

    public int GetScore(Team team) {
        if (team == Team.Red) return RedScore.Value;
        if (team == Team.Blue) return BlueScore.Value;
        return 0;
    }

    // ===============================
    // Utility
    // ===============================
    public bool TryGetTeam(ulong? clientId, out Team team) {
        if (clientId == null) {
            team = Team.None;
            return false;
        }

        foreach (var entry in _teams) {
            if (entry.clientId == clientId) {
                team = entry.team;
                return true;
            }
        }

        team = Team.None;
        return false;
    }

    public bool HasTeam(ulong clientId) {
        return TryGetTeam(clientId, out _);
    }

    public bool HasTeam(ParticipantId participantId) {
        return TryGetTeam(participantId, out _);
    }

    public bool TryGetLocalTeam(out Team team) {
        if (NetworkManager.Singleton == null) {
            team = Team.None;
            return false;
        }

        return TryGetTeam(NetworkManager.Singleton.LocalClientId, out team);
    }

    public Team GetTeam(ulong? clientId) {
        return TryGetTeam(clientId, out var team) ? team : Team.None;
    }

    public bool TryGetTeam(ParticipantId participantId, out Team team) {
        if (participantId.IsHuman)
            return TryGetTeam(participantId.Value, out team);

        return _botTeams.TryGetValue(participantId.Value, out team);
    }

    public Team GetTeam(ParticipantId participantId) {
        return TryGetTeam(participantId, out var team) ? team : Team.None;
    }

    public List<ulong> FindAllies(ulong clientId) {
        var result = new List<ulong>();
        var team = GetTeam(clientId);

        foreach (var entry in _teams) {
            if (entry.team == team)
                result.Add(entry.clientId);
        }

        return result;
    }

    public bool AreEnemies(ulong a, ulong b) {
        return AreEnemies(ParticipantOwnerCodec.Decode(a), ParticipantOwnerCodec.Decode(b));
    }

    public bool AreEnemies(ParticipantId a, ParticipantId b) {
        if (!isTeamMode)
            return a != b;
        return GetTeam(a) != GetTeam(b);
    }

    public bool AreAllies(ulong a, ulong b) {
        return !AreEnemies(a, b);
    }

    public bool AreAllies(ParticipantId a, ParticipantId b) {
        return !AreEnemies(a, b);
    }
}