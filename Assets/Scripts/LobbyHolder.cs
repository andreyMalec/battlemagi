using System;
using Steamworks.Data;
using UnityEngine;

public class LobbyHolder : MonoBehaviour {
    public Lobby? currentLobby;
    public static LobbyHolder instance;

    private void Awake() {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}