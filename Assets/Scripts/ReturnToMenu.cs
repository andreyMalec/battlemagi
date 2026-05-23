using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.KeypadPlus;

    private void Update() {
        if (Input.GetKeyDown(key)) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue)
                Leave();
        }
    }

    private void Leave() {
        TeamManager.Instance.Reset();
        LobbyManager.Instance.LeaveLobby();
        SceneLoader.LoadMenu();
    }
}