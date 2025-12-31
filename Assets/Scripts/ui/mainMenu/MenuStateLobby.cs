using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private TMP_Dropdown dropdownGameEnd;
    [SerializeField] private TMP_Dropdown dropdownKeyCast;
    [SerializeField] private TMP_Text gameEndTarget;
    [SerializeField] private TMP_Text lobbyName;
    [SerializeField] private TMP_InputField fieldLobbyId;
    private TMP_Text copyButtonText;

    private int readyCount = 0;

    private DropdownHelper _dropdownMapHelper;
    private DropdownHelper _dropdownModeHelper;
    private DropdownHelper _dropdownGameEndHelper;
    private DropdownHelper _dropdownKeyCastHelper;

    private void Awake() {
        _dropdownMapHelper = dropdownMap.GetComponent<DropdownHelper>();
        _dropdownModeHelper = dropdownMode.GetComponent<DropdownHelper>();
        _dropdownGameEndHelper = dropdownGameEnd.GetComponent<DropdownHelper>();
        _dropdownKeyCastHelper = dropdownKeyCast.GetComponent<DropdownHelper>();

        buttonBackToMain.onClick.AddListener(LeaveLobby);
        buttonInvite.onClick.AddListener(InviteFriends);
        buttonReady.onClick.AddListener(ToggleReady);
        buttonCopyLobbyId.onClick.AddListener(() => StartCoroutine(CopyId()));
        copyButtonText = buttonCopyLobbyId.GetComponentInChildren<TMP_Text>();
        dropdownMap.onValueChanged.AddListener(SubmitMap);
        dropdownMode.onValueChanged.AddListener(SubmitMode);
        dropdownGameEnd.onValueChanged.AddListener(SubmitEndChoice);
        dropdownKeyCast.onValueChanged.AddListener(SubmitKeyCast);

        UpdateGameEndOptions(dropdownMode.value);
    }

    private void OnEnable() {
        UpdateReadyButton(LobbyManager.Instance.Me.IsReady());

        dropdownMode.ClearOptions();
        var freeForAll = R.String("gameMode.freeForAll");
        var teamDeathmatch = R.String("gameMode.teamDeathmatch");
        var captureTheFlag = R.String("gameMode.captureTheFlag");
        dropdownMode.AddOptions(new List<string> { freeForAll, teamDeathmatch, captureTheFlag });

        dropdownMap.options = GameMapDatabase.instance.gameMaps
            .Map(it => new TMP_Dropdown.OptionData(R.String($"map.{it.mapName}")))
            .ToList();
        dropdownKeyCast.options = new List<TMP_Dropdown.OptionData>() {
            new(R.String("lobby.keyCast.disabled")),
            new(R.String("lobby.keyCast.enabled")),
        };
        UpdateGameEndTargetText();
    }

    private void UpdateMap() {
        dropdownMap.captionText.text = dropdownMap.options[GameProgress.Instance.SelectedMap.Value].text;
    }

    private void UpdateTeamMode() {
        dropdownMode.captionText.text = dropdownMode.options[(int)TeamManager.Instance.CurrentMode.Value].text;
    }

    private void UpdateGameEnd() {
        dropdownGameEnd.captionText.text = dropdownGameEnd.options[TeamManager.Instance.EndChoice.Value].text;
    }

    private void FixedUpdate() {
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue) return;

        fieldLobbyId.text = lobby?.Id.ToString();
        readyCount = lobby.ReadyCount();
        var playersCount = $"{lobby?.MemberCount}/{lobby?.MaxMembers}";
        lobbyName.text = R.String("lobby.players", playersCount, readyCount.ToString());
        if (readyCount == lobby?.MemberCount) {
            StartGame();
        }

        dropdownKeyCast.value = GameConfig.Instance.allowKeySpells ? 1 : 0;

        var showControls = LobbyManager.Instance.IsHost();
        _dropdownMapHelper.SetInteractable(showControls);
        _dropdownModeHelper.SetInteractable(showControls);
        _dropdownGameEndHelper.SetInteractable(showControls);
        _dropdownKeyCastHelper.SetInteractable(showControls);
        UpdateGameEndTargetText();
        UpdateMap();
        UpdateTeamMode();
        UpdateGameEnd();
    }

    private void StartGame() {
        GameProgress.Instance.StartMatch();
    }

    private void SubmitMap(int index) {
        GameProgress.Instance.SelectMap(index);
    }

    private void SubmitMode(int index) {
        TeamManager.Instance.SetMode((TeamManager.TeamMode)index);
        UpdateGameEndOptions(index);
    }

    private void SubmitEndChoice(int index) {
        TeamManager.Instance.SetEndChoice(index);
    }

    private void SubmitKeyCast(int index) {
        GameProgress.Instance.SetKeyCast(index == 1);
    }

    private void UpdateGameEndOptions(int modeIndex) {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        if (modeIndex == (int)TeamManager.TeamMode.CaptureTheFlag) {
            foreach (var v in GameProgress.ctfTargets)
                options.Add(new TMP_Dropdown.OptionData(v.ToString()));
        } else {
            foreach (var v in GameProgress.killsTargets)
                options.Add(new TMP_Dropdown.OptionData(v.ToString()));
        }

        dropdownGameEnd.options = options;
        dropdownGameEnd.value = 0;
    }

    private void UpdateGameEndTargetText() {
        int modeIndex = dropdownMode.value;
        gameEndTarget.text = modeIndex == (int)TeamManager.TeamMode.CaptureTheFlag
            ? R.String("lobby.targetFlags")
            : R.String("lobby.targetKills");
    }

    private void ToggleReady() {
        UpdateReadyButton(LobbyManager.Instance.ToggleReady());
    }

    private void UpdateReadyButton(bool ready) {
        if (ready) {
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