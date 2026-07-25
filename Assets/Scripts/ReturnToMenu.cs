using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.KeypadPlus;

    private void Update() {
        if (Input.GetKeyDown(key)) {
            var lobby = Ctx.CurrentLobby;
            if (lobby.HasValue)
                Leave();
        }
    }

    private void Leave() {
        Ctx.Teams.Reset();
        Ctx.LeaveLobby();
        SceneLoader.LoadMenu();
    }
}