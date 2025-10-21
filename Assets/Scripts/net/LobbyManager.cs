using System;
using System.Collections.Generic;
using System.Linq;
using Netcode.Transports.Facepunch;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour {
    public static readonly string KeyReady = "Ready";
    public static readonly string KeyColor = "Color";
    public static LobbyManager Instance { get; private set; }

    public enum PlayerState {
        Disconnected,
        InLobby,
        InGame
    }

    private PlayerState _state = PlayerState.Disconnected;

    public PlayerState State {
        get => _state;
        private set {
            if (value == _state) return;
            _state = value;
            OnStateChanged?.Invoke(value);
        }
    }

    public event Action<PlayerState> OnStateChanged;

    // Текущее лобби, если мы в нём
    public Lobby? CurrentLobby { get; private set; }
    public Friend Me { get; private set; }

    // Все найденные лобби (обновляются при поиске)
    public List<Lobby> AvailableLobbies { get; private set; } = new();

    private SteamId? lobbyOwner = null;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        RegisterCallbacks();
    }

    private void OnDestroy() {
        UnregisterCallbacks();
    }

    #region Steam Lobby Events

    private void RegisterCallbacks() {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnChatMessage += OnLobbyChatMessage;
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void UnregisterCallbacks() {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnChatMessage -= OnLobbyChatMessage;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }

    private void OnLobbyCreated(Result result, Lobby lobby) {
        Debug.Log($"[LobbyManager] Lobby created: {result}, id={lobby.Id}");
        if (result == Result.OK) {
            CurrentLobby = lobby;
            lobbyOwner = lobby.Owner.Id;
            State = PlayerState.InLobby;
            lobby.SetPublic();
            lobby.SetJoinable(true);
            NetworkManager.Singleton.StartHost();
        }
    }

    private void OnLobbyEntered(Lobby lobby) {
        Debug.Log($"[LobbyManager] Entered lobby {lobby.Id}");
        CurrentLobby = lobby;
        lobbyOwner = lobby.Owner.Id;
        State = PlayerState.InLobby;

        Me = lobby.Members.FirstOrDefault(m => m.Id == SteamClient.SteamId);

        lobby.SendChatString($"{Me.Name} entered lobby {lobby.Id}");

        if (NetworkManager.Singleton.IsHost)
            return;
        NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend member) {
        Debug.Log($"[LobbyManager] {member.Name} joined lobby {lobby.Id}");
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend member) {
        Debug.Log($"[LobbyManager] {member.Name} left lobby {lobby.Id}");
        // lobby.Owner.Id Переходит к другому игроку, но мы просто кикаем всех вместе с хостом
        if (lobbyOwner == member.Id)
            LeaveLobby();
    }

    private void OnLobbyChatMessage(Lobby lobby, Friend member, string message) {
        Debug.Log($"[Chat] {member.Name}: {message}");
    }

    private void OnLobbyInvite(Friend friend, Lobby lobby) {
        Debug.Log($"[LobbyManager] Received invite from {friend.Name} to lobby {lobby.Id}");
    }

    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId) {
        await lobby.Join();
    }

    #endregion

    #region Public API

    public bool IsHost() {
        return CurrentLobby.HasValue && CurrentLobby.Value.Owner.Id == SteamClient.SteamId;
    }

    /**
     * Создать новое лобби
     */
    public async void CreateLobby(int maxPlayers) {
        Debug.Log("[LobbyManager] Creating new lobby...");
        await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
    }

    public void InviteFriends() {
        if (CurrentLobby.HasValue)
            SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
    }

    /**
     * Войти в лобби по ID
     */
    public async void JoinLobby(ulong lobbyId) {
        Debug.Log($"[LobbyManager] Joining lobby {lobbyId}...");
        await SteamMatchmaking.JoinLobbyAsync(lobbyId);
    }

    /**
     * Найти все публичные лобби
     */
    public async void RefreshLobbyList() {
        Debug.Log("[LobbyManager] Refreshing lobbies...");
        var list = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

        AvailableLobbies.Clear();
        if (list != null) {
            AvailableLobbies.AddRange(list);
            Debug.Log($"[LobbyManager] Found {AvailableLobbies.Count} lobbies");
        }
    }

    /**
     * @returns isReady
     */
    public bool ToggleReady() {
        var lobby = CurrentLobby;
        if (lobby == null)
            return false;

        var me = Me;
        var last = lobby.Value.GetMemberData(me, KeyReady);
        if (last == "1") {
            lobby.Value.SetMemberData(KeyReady, "0");
            Debug.Log($"{me.Name} [{me.Id}] is now NOT_READY");
            return false;
        }

        lobby.Value.SetMemberData(KeyReady, "1");
        Debug.Log($"{me.Name} [{me.Id}] is now READY");
        return true;
    }

    /**
     * Покинуть текущее лобби
     */
    public void LeaveLobby() {
        if (CurrentLobby != null) {
            var lobby = CurrentLobby.Value;
            Debug.Log($"[LobbyManager] Leaving lobby {lobby.Id}");
            lobby.Leave();
            CurrentLobby = null;
            lobbyOwner = null;
        }

        State = PlayerState.Disconnected;
        NetworkManager.Singleton.Shutdown();
    }

    #endregion
}

public static class LobbyExt {
    public static string Name(this PlayerManager.PlayerData player) {
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby == null) return null;

        foreach (var member in lobby.Value.Members) {
            if (player.SteamId == member.Id)
                return member.Name;
        }

        return null;
    }

    public static int ReadyCount(this Lobby? lobby) {
        var lobbyReadyCount = 0;
        if (lobby.HasValue) {
            foreach (var member in lobby.Value.Members) {
                var ready = lobby.Value.GetMemberData(member, LobbyManager.KeyReady);
                if (ready == "1") {
                    lobbyReadyCount++;
                }
            }
        }

        return lobbyReadyCount;
    }

    public static bool IsReady(this Friend member) {
        if (LobbyManager.Instance.CurrentLobby != null) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue) {
                var ready = lobby.Value.GetMemberData(member, LobbyManager.KeyReady);
                return ready == "1";
            }
        }

        return false;
    }

    public static PlayerColor GetColor(this Friend member) {
        if (LobbyManager.Instance.CurrentLobby != null) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue) {
                var color = lobby.Value.GetMemberData(member, LobbyManager.KeyColor);
                if (string.IsNullOrEmpty(color))
                    color = "78,0;0,5";
                var split = color.Split(";");
                var h = float.Parse(split[0]);
                var s = float.Parse(split[1]);
                return new PlayerColor(h, s);
            }
        }

        return new PlayerColor(78f, 0.5f);
    }

    public static void SetColor(PlayerColor color) {
        if (LobbyManager.Instance.CurrentLobby != null) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue) {
                var c = $"{color.hue};{color.saturation}";
                lobby.Value.SetMemberData(LobbyManager.KeyColor, c);
            }
        }
    }

    public static TeamManager.Team GetTeam(this Friend member) {
        var player = PlayerManager.Instance.FindBySteamId(member.Id);
        return TeamManager.Instance.GetTeam(player?.ClientId);
    }

    public static void SetTeam(TeamManager.Team team) {
        TeamManager.Instance.RequestChangeTeamServerRpc(NetworkManager.Singleton.LocalClientId, (int)team);
    }
}

public struct PlayerColor {
    public float hue;
    public float saturation;

    public PlayerColor(float hue, float saturation) {
        this.hue = hue;
        this.saturation = saturation;
    }
}