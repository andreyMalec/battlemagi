using System;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour {
    private void Update() {
        if (Input.GetKeyDown(KeyCode.F2)) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue)
                Leave();
        }
    }

    private void Leave() {
        LobbyManager.Instance.LeaveLobby();
        SceneManager.LoadScene("MainMenu");
    }
}