using System;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour {
    private static readonly int StandUp = Animator.StringToHash("Stand Up");
    private static readonly int SitDown = Animator.StringToHash("Sit Down");

    [Header("States")]
    [SerializeField] private GameObject mainState;

    [SerializeField] private GameObject lobbyState;
    [SerializeField] private GameObject settingsState;
    [SerializeField] private GameObject creditsState;

    [Header("GUI")]
    [SerializeField] private LobbyMembers lobbyMembers;

    [SerializeField] private Button buttonSettings;
    [SerializeField] private Button buttonCredits;

    [Header("Character")]
    [SerializeField] private Animator animator;

    private State _state;

    private enum State {
        Main,
        Lobby,
        Settings,
        Credits
    }

    public void BackToMain() {
        _state = State.Main;
    }

    private void Awake() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        buttonSettings.onClick.AddListener(() => _state = State.Settings);
        buttonCredits.onClick.AddListener(() => _state = State.Credits);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && (_state == State.Settings || _state == State.Credits)) {
            BackToMain();
        }
    }

    private void FixedUpdate() {
        mainState.SetActive(_state == State.Main);
        lobbyState.SetActive(_state == State.Lobby);
        settingsState.SetActive(_state == State.Settings);
        creditsState.SetActive(_state == State.Credits);
    }

    private void OnEnable() {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnDisable() {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(LobbyManager.PlayerState state) {
        var newValue = state == LobbyManager.PlayerState.InLobby;
        if (newValue && _state != State.Lobby) {
            animator.SetTrigger(StandUp);
            _state = State.Lobby;
        }

        if (!newValue && _state == State.Lobby) {
            animator.SetTrigger(SitDown);
            _state = State.Main;
        }


        lobbyMembers.RequestUpdate();
    }
}