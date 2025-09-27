using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

public class Menu : MonoBehaviour {
    private static readonly int StandUp = Animator.StringToHash("Stand Up");
    private static readonly int SitDown = Animator.StringToHash("Sit Down");

    [Header("States")]
    public GameObject mainState;

    public GameObject lobbyState;

    [Header("GUI")]
    public Button exit;

    public Button buttonMakeLobby;
    public Button buttonJoinLobby;
    public Button buttonBackToMain;
    public Button buttonCopyLobbyId;
    public Button buttonReady;
    public TMP_Text lobbyName;
    public TMP_InputField fieldJoinLobbyId;
    public TMP_InputField fieldLobbyId;
    private TMP_Text copyButtonText;

    public LobbyMembers lobbyMembers;

    [Header("Character")]
    public Animator animator;

    private bool inLobby = false;
    private UInt64 lobbyId = 0;
    private int lobbySize = 4;
    private int readyCount = 0;

    public void Start() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        exit.onClick.AddListener(Application.Quit);
        buttonBackToMain.onClick.AddListener(LeaveLobby);
        buttonJoinLobby.onClick.AddListener(JoinLobby);
        buttonMakeLobby.onClick.AddListener(CreateLobby);
        buttonReady.onClick.AddListener(ToggleReady);
        buttonCopyLobbyId.onClick.AddListener(() => StartCoroutine(CopyId()));
        copyButtonText = buttonCopyLobbyId.GetComponentInChildren<TMP_Text>();

        fieldJoinLobbyId.onEndEdit.AddListener(id => {
            try {
                lobbyId = Convert.ToUInt64(id);
            } catch (FormatException e) {
                lobbyId = 0;
                fieldJoinLobbyId.text = "";
                Console.WriteLine(e);
            }
        });
    }

    private void FixedUpdate() {
        if (inLobby) {
            mainState.SetActive(false);
            lobbyState.SetActive(true);

            var lobby = LobbyManager.Instance.CurrentLobby;

            readyCount = lobby.ReadyCount();
            lobbyName.text = $"Players {lobby?.MemberCount}/{lobbySize}; Ready {readyCount}";
            if (readyCount == lobby?.MemberCount) {
                StartGame();
            }
        } else {
            mainState.SetActive(true);
            lobbyState.SetActive(false);
        }
    }

    private void ToggleReady() {
        if (LobbyManager.Instance.ToggleReady()) {
            buttonReady.GetComponent<Image>().color = Color.chartreuse;
        } else {
            buttonReady.GetComponent<Image>().color = Color.white;
        }
    }

    private void StartGame() {
        GameScene.StartGame();
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

    private void LeaveLobby() {
        LobbyManager.Instance.LeaveLobby();
        buttonReady.GetComponent<Image>().color = Color.white;
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
        if (newValue && !inLobby) {
            animator.SetTrigger(StandUp);
        }

        if (!newValue && inLobby) {
            animator.SetTrigger(SitDown);
        }

        inLobby = newValue;
        if (inLobby) {
            var lobby = LobbyManager.Instance.CurrentLobby.Value;
            fieldLobbyId.text = lobby.Id.ToString();
            lobbySize = lobby.MaxMembers;
            lobbyId = lobby.Id.Value;
        }

        lobbyMembers.RequestUpdate();
    }

    private IEnumerator CopyId() {
        GUIUtility.systemCopyBuffer = lobbyId.ToString();
        copyButtonText.text = "OK";
        yield return new WaitForSeconds(1);
        copyButtonText.text = "Copy";
    }
}