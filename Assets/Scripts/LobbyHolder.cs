using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class LobbyHolder : MonoBehaviour {
    public Lobby? currentLobby;
    public Friend me;
    public static LobbyHolder instance;

    private void Awake() {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit() {
        currentLobby?.Leave();
    }
}