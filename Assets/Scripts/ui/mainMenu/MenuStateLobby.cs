using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuStateLobby : MonoBehaviour {
    [SerializeField] private Button buttonInvite;
    [SerializeField] private Button buttonBackToMain;
    [SerializeField] private Button buttonCopyLobbyId;
    [SerializeField] private Button buttonReady;
    [SerializeField] private TMP_Dropdown dropdownMap;
    [SerializeField] private TMP_Dropdown dropdownMode;
    [SerializeField] private TMP_Text lobbyName;
    [SerializeField] private TMP_InputField fieldLobbyId;
    private TMP_Text copyButtonText;

    private int readyCount = 0;
    private int mapIndex = 0;

    private void Awake() {
        buttonBackToMain.onClick.AddListener(LeaveLobby);
        buttonInvite.onClick.AddListener(InviteFriends);
        buttonReady.onClick.AddListener(ToggleReady);
        buttonCopyLobbyId.onClick.AddListener(() => StartCoroutine(CopyId()));
        copyButtonText = buttonCopyLobbyId.GetComponentInChildren<TMP_Text>();
        dropdownMap.onValueChanged.AddListener(SubmitMap);
        dropdownMode.onValueChanged.AddListener(SubmitMode);
    }

    private void FixedUpdate() {
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue) return;

        fieldLobbyId.text = lobby?.Id.ToString();
        readyCount = lobby.ReadyCount();
        lobbyName.text = $"Players {lobby?.MemberCount}/{lobby?.MaxMembers}; Ready {readyCount}";
        if (readyCount == lobby?.MemberCount) {
            StartGame();
        }

        var showControls = LobbyManager.Instance.IsHost();
        dropdownMap.gameObject.SetActive(showControls);
        dropdownMode.gameObject.SetActive(showControls);
    }

    private void StartGame() {
        GameScene.StartGame(mapIndex);
    }

    private void SubmitMap(int index) {
        mapIndex = index;
    }

    private void SubmitMode(int index) {
        TeamManager.Instance.SetModeServerRpc((TeamManager.TeamMode)index);
    }

    private void ToggleReady() {
        if (LobbyManager.Instance.ToggleReady()) {
            buttonReady.GetComponent<Image>().color = Color.chartreuse;
        } else {
            buttonReady.GetComponent<Image>().color = Color.white;
        }
    }

    private void InviteFriends() {
        LobbyManager.Instance.InviteFriends();
    }

    private void LeaveLobby() {
        LobbyManager.Instance.LeaveLobby();
        buttonReady.GetComponent<Image>().color = Color.white;
        dropdownMap.value = 0;
        dropdownMode.value = 0;
    }

    private IEnumerator CopyId() {
        GUIUtility.systemCopyBuffer = LobbyManager.Instance.CurrentLobby?.Id.ToString();
        copyButtonText.text = "OK";
        yield return new WaitForSeconds(1);
        copyButtonText.text = "Copy";
    }
}