using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.Keypad0;

    private void Update() {
        if (Input.GetKeyDown(key)) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue)
                Leave();
        }
    }

    private void Leave() {
        LobbyManager.Instance.LeaveLobby();
        TeamManager.Instance.Reset();
        SceneManager.LoadScene("MainMenu");
    }
}