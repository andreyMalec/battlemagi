using System;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour {
    private void OnEnable() {
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
    }

    private void OnDisable() {
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberDisconnected;
        SteamMatchmaking.OnLobbyMemberKicked += OnLobbyMemberKicked;
    }

    private void OnLobbyMemberKicked(Lobby lobby, Friend member, Friend owner) {
        Debug.Log($"{owner.Name} kicked {member.Name}");
    }

    private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend) {
        Debug.Log($"{friend.Name} disconnected");
        if (lobby.Owner.Id == friend.Id) {
            Leave(lobby);
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F2)) {
            var lobby = LobbyHolder.instance.currentLobby;
            if (lobby.HasValue)
                Leave(lobby.Value);
        }
    }

    private void Leave(Lobby lobby) {
        lobby.Leave();
        LobbyHolder.instance.currentLobby = null;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}