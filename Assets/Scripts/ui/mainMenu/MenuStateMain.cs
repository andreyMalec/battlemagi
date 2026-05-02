using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuStateMain : MonoBehaviour {
    [SerializeField] private Button buttonMakeLobby;
    [SerializeField] private Button buttonFindLobby;
    [SerializeField] private Button buttonExit;
    [SerializeField] private Button buttonJoinLobby;
    [SerializeField] private TMP_InputField fieldJoinLobbyId;
    [SerializeField] private Menu menu;

    private UInt64 lobbyId = 0;
    private const int lobbySize = 8;

    private void Awake() {
        buttonExit.onClick.AddListener(Application.Quit);
        buttonJoinLobby.onClick.AddListener(JoinLobby);
        buttonMakeLobby.onClick.AddListener(CreateLobby);
        buttonFindLobby.onClick.AddListener(OpenFindLobby);

        fieldJoinLobbyId.onEndEdit.AddListener(id => {
            try {
                lobbyId = Convert.ToUInt64(id);
            } catch (FormatException e) {
                lobbyId = 0;
                fieldJoinLobbyId.text = "";
            }
        });
    }

    private void CreateLobby() {
        LobbyManager.Instance.CreateLobby(lobbySize);
    }

    private void JoinLobby() {
        if (lobbyId == 0) {
            Debug.LogWarning("lobbyId is empty");
            return;
        }

        LobbyManager.Instance.JoinLobby(lobbyId);
    }

    private void OpenFindLobby() {
        menu.OpenFindLobby();
    }
}