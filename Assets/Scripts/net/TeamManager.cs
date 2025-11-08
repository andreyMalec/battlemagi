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
        RedistributePlayers();
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn() {
        if (NetworkManager != null) {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ===============================
    // SERVER: Подключение/Отключение
    // ===============================
    private void OnClientConnected(ulong clientId) {
        if (!IsServer) return;

        Team team = AssignTeam(clientId);
        _teams.Add(new TeamEntry { clientId = clientId, team = team });

        Debug.Log($"[TeamManager] {clientId} joined team {team}");
    }

    private void OnClientDisconnected(ulong clientId) {
        if (!IsServer) return;

        int index = _teams.ToList().FindIndex(e => e.clientId == clientId);
        if (index >= 0) _teams.RemoveAt(index);
    }

    private Team AssignTeam(ulong clientId) {
        if (!isTeamMode)
            return (Team)clientId; // уникальный ID — каждый сам за себя

        // TwoTeams — добавляем в менее заполненную
        int red = 0;
        int blue = 0;
        foreach (var entry in _teams) {
            if (entry.team == Team.Red)
                red++;
            if (entry.team == Team.Blue)
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

    private void RedistributePlayers() {
        foreach (var entry in _teams) {
            Team newTeam = AssignTeam(entry.clientId);

            int idx = _teams.ToList().FindIndex(e => e.clientId == entry.clientId);
            if (idx >= 0)
                _teams[idx] = new TeamEntry { clientId = entry.clientId, team = newTeam };
        }

        Debug.Log($"[TeamManager] Rebalanced into {CurrentMode.Value}");
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
        int index = _teams.ToList().FindIndex(e => e.clientId == clientId);
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
    public Team GetTeam(ulong? clientId) {
        if (clientId == null) return Team.None;

        foreach (var entry in _teams) {
            if (entry.clientId == clientId)
                return entry.team;
        }

        return Team.None;
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
        if (!isTeamMode)
            return a != b;
        return GetTeam(a) != GetTeam(b);
    }

    public bool AreAllies(ulong a, ulong b) {
        return !AreEnemies(a, b);
    }
}