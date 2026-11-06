using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour {
    private static readonly int StandUp = Animator.StringToHash("Stand Up");
    private static readonly int SitDown = Animator.StringToHash("Sit Down");

    [Header("States")]
    [SerializeField] private GameObject mainState;

    [SerializeField] private GameObject lobbyState;
    [SerializeField] private GameObject findLobbyState;
    [SerializeField] private GameObject runeCraftState;
    [SerializeField] private GameObject settingsState;
    [SerializeField] private GameObject creditsState;

    [Header("GUI")]
    [SerializeField] private LobbyMembers lobbyMembers;

    [SerializeField] private Button buttonSettings;
    [SerializeField] private Button buttonRuneCraft;
    [SerializeField] private Button buttonCredits;

    [Header("Character")]
    [SerializeField] private Transform avatarRoot;

    private State _state;

    private enum State {
        Main,
        Lobby,
        FindLobby,
        RuneCraft,
        Settings,
        Credits
    }

    public void BackToMain() {
        _state = State.Main;
    }

    public void OpenFindLobby() {
        _state = State.FindLobby;
    }

    private void Awake() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        buttonRuneCraft.onClick.AddListener(() => _state = State.RuneCraft);
        buttonSettings.onClick.AddListener(() => _state = State.Settings);
        buttonCredits.onClick.AddListener(() => _state = State.Credits);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) &&
            (_state == State.Settings || _state == State.Credits || _state == State.FindLobby)) {
            BackToMain();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _state == State.RuneCraft &&
            !runeCraftState.GetComponentInChildren<RuneCraftUiController>().OnBackClicked()) {
            BackToMain();
        }
    }

    private void FixedUpdate() {
        mainState.SetActive(_state == State.Main);
        lobbyState.SetActive(_state == State.Lobby);
        findLobbyState.SetActive(_state == State.FindLobby);
        runeCraftState.SetActive(_state == State.RuneCraft);
        settingsState.SetActive(_state == State.Settings);
        creditsState.SetActive(_state == State.Credits);
    }

    private void OnEnable() {
        if (Ctx.Lobby == null) return;
        Ctx.Lobby.OnStateChanged += OnStateChanged;
        OnStateChanged(Ctx.LobbyState);
    }

    private void OnDisable() {
        if (Ctx.Lobby != null)
            Ctx.Lobby.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(LobbyManager.PlayerState state) {
        var newValue = state == LobbyManager.PlayerState.InLobby || state == LobbyManager.PlayerState.InGame;
        if (newValue && _state != State.Lobby) {
            StartCoroutine(UpdateAvatar(true));
            _state = State.Lobby;
        }

        if (!newValue && _state == State.Lobby) {
            StartCoroutine(UpdateAvatar(false));
            _state = State.Main;
        }


        lobbyMembers.RequestUpdate();
    }

    private IEnumerator UpdateAvatar(bool up) {
        Animator animator = null;
        yield return new WaitUntil(() => {
            animator = avatarRoot.GetComponentInChildren<Animator>();
            return animator != null;
        });

        animator.SetTrigger(up ? StandUp : SitDown);
    }
}