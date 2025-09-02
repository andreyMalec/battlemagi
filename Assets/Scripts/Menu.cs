using System;
using System.Collections;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

public class Menu : MonoBehaviour {
    [Header("States")] public GameObject mainState;
    public GameObject lobbyState;


    [Header("GUI")] public Button exit;
    public Button buttonMakeLobby;
    public Button buttonJoinLobby;
    public Button buttonBackToMain;
    public Button buttonCopyLobbyId;
    public Button buttonReady;
    public TMP_Text lobbyName;
    public TMP_InputField fieldJoinLobbyId;
    public TMP_InputField fieldLobbyId;
    private TMP_Text copyButtonText;

    private bool inLobby = false;
    private UInt64 lobbyId = 0;
    private int lobbySize = 4;
    private int playerCount = 0;
    private int readyCount = 0;

    string m_SessionTicket;
    string identity = "unityauthenticationservice";

    public void Start() {
        exit.onClick.AddListener(Application.Quit);
        buttonBackToMain.onClick.AddListener(LeaveLobby);
        buttonJoinLobby.onClick.AddListener(JoinLobby);
        buttonMakeLobby.onClick.AddListener(CreateLobby);
        buttonReady.onClick.AddListener(ToggleReady);
        buttonCopyLobbyId.onClick.AddListener(() => StartCoroutine(CopyId()));
        copyButtonText = buttonCopyLobbyId.GetComponentInChildren<TMP_Text>();

        SteamUser.OnValidateAuthTicketResponse += (id, steamId, arg3) => {
            Debug.Log($"id={id}, steamId={steamId}, arg3={arg3}");
        };

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

            var lobby = LobbyHolder.instance.currentLobby;

            var lobbyReadyCount = 0;
            if (lobby.HasValue) {
                foreach (var member in lobby.Value.Members) {
                    var ready = lobby.Value.GetMemberData(member, "Ready");
                    if (ready == "1") {
                        lobbyReadyCount++;
                    }
                }
            }

            readyCount = lobbyReadyCount;
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
        var lobby = LobbyHolder.instance.currentLobby;
        if (lobby == null)
            return;

        foreach (var member in lobby.Value.Members) {
            if (member.Id == SteamClient.SteamId) {
                var last = lobby.Value.GetMemberData(member, "Ready");
                if (last == "1") {
                    lobby.Value.SetMemberData("Ready", "0");
                    buttonReady.GetComponent<Image>().color = Color.white;
                    Debug.Log($"{member.Name} [{member.Id}] is now NOT_READY");
                } else {
                    lobby.Value.SetMemberData("Ready", "1");
                    buttonReady.GetComponent<Image>().color = Color.chartreuse;
                    Debug.Log($"{member.Name} [{member.Id}] is now READY");
                }

                break;
            }
        }
    }

    private void StartGame() {
        if (NetworkManager.Singleton.IsHost) {
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }

    private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId) {
        await lobby.Join();
    }

    private void OnLobbyEntered(Lobby lobby) {
        LobbyHolder.instance.currentLobby = lobby;
        fieldLobbyId.text = lobby.Id.ToString();

        inLobby = true;
        lobbySize = lobby.MaxMembers;

        lobbyId = lobby.Id.Value;
        Debug.Log($"lobby {lobbyId} entered");

        if (NetworkManager.Singleton.IsHost)
            return;
        NetworkManager.Singleton.gameObject.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();
    }

    private void OnLobbyCreated(Result result, Lobby lobby) {
        if (result == Result.OK) {
            Debug.Log($"lobby {lobby.Id.ToString()} created");
            lobby.SetPublic();
            lobby.SetJoinable(true);
            lobby.SetData("ReadyCount", "0");
            NetworkManager.Singleton.StartHost();
        } else {
            Debug.LogWarning("lobby creation result is not OK: " + result);
        }
    }

    private async void CreateLobby() {
        await SteamMatchmaking.CreateLobbyAsync(lobbySize);
    }

    private async void JoinLobby() {
        if (lobbyId == 0) {
            Debug.LogWarning("lobbyId is empty");
            return;
        }

        await SteamMatchmaking.JoinLobbyAsync(lobbyId);
    }

    private void LeaveLobby() {
        inLobby = false;
        LobbyHolder.instance.currentLobby?.Leave();
        LobbyHolder.instance.currentLobby = null;
        NetworkManager.Singleton.Shutdown();
    }

    private void OnEnable() {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnChatMessage += OnChatMessage;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDisable() {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnChatMessage -= OnChatMessage;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }

    private void OnChatMessage(Lobby lobby, Friend friend, string text) {
        Debug.Log($"{friend.Name}: {text}");
    }

    private IEnumerator CopyId() {
        GUIUtility.systemCopyBuffer = lobbyId.ToString();
        copyButtonText.text = "OK";
        yield return new WaitForSeconds(2);
        copyButtonText.text = "Copy";
    }
}