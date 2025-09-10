using System;
using System.Collections.Generic;
using System.Linq;
using Netcode.Transports.Facepunch;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour {
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

    /**
     * Создать новое лобби
     */
    public async void CreateLobby(int maxPlayers) {
        Debug.Log("[LobbyManager] Creating new lobby...");
        await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
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
        var last = lobby.Value.GetMemberData(me, "Ready");
        if (last == "1") {
            lobby.Value.SetMemberData("Ready", "0");
            Debug.Log($"{me.Name} [{me.Id}] is now NOT_READY");
            return false;
        }

        lobby.Value.SetMemberData("Ready", "1");
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
    public static int ReadyCount(this Lobby? lobby) {
        var lobbyReadyCount = 0;
        if (lobby.HasValue) {
            foreach (var member in lobby.Value.Members) {
                var ready = lobby.Value.GetMemberData(member, "Ready");
                if (ready == "1") {
                    lobbyReadyCount++;
                }
            }
        }

        return lobbyReadyCount;
    }
}