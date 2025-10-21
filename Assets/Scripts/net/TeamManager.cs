using System;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour {
    public enum TeamMode {
        FreeForAll,
        TwoTeams
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

    // ========== Sync data ==========
    public NetworkVariable<TeamMode> CurrentMode = new(TeamMode.FreeForAll);
    private NetworkList<TeamEntry> _teams;

    public static TeamManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
        _teams = new NetworkList<TeamEntry>();
        _teams.OnListChanged += OnTeamListChanged;
    }

    public override void OnNetworkSpawn() {
        if (IsServer) {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy() {
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
        if (CurrentMode.Value == TeamMode.FreeForAll)
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

    // ===============================
    // SERVER: Переключение режима
    // ===============================
    [ServerRpc(RequireOwnership = false)]
    public void SetModeServerRpc(TeamMode mode) {
        if (!IsServer) return;
        CurrentMode.Value = mode;
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
        if (CurrentMode.Value == TeamMode.TwoTeams && (newTeamId < 0 || newTeamId > 1))
            return;
        entry.team = (Team)newTeamId;
        _teams[index] = entry;

        Debug.Log($"[TeamManager] Player {clientId} switched to team {newTeamId}");
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

    public bool AreEnemies(ulong a, ulong b) {
        if (CurrentMode.Value == TeamMode.FreeForAll)
            return a != b;
        return GetTeam(a) != GetTeam(b);
    }
}