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
    [SerializeField] private TMP_Text gameEndTarget;
    [SerializeField] private TMP_Text lobbyName;
    [SerializeField] private TMP_InputField fieldLobbyId;
    private TMP_Text copyButtonText;

    private int readyCount = 0;

    private DropdownHelper _dropdownMapHelper;
    private DropdownHelper _dropdownModeHelper;
    private DropdownHelper _dropdownGameEndHelper;

    private void Awake() {
        _dropdownMapHelper = dropdownMap.GetComponent<DropdownHelper>();
        _dropdownModeHelper = dropdownMode.GetComponent<DropdownHelper>();
        _dropdownGameEndHelper = dropdownGameEnd.GetComponent<DropdownHelper>();

        buttonBackToMain.onClick.AddListener(LeaveLobby);
        buttonInvite.onClick.AddListener(InviteFriends);
        buttonReady.onClick.AddListener(ToggleReady);
        buttonCopyLobbyId.onClick.AddListener(() => StartCoroutine(CopyId()));
        copyButtonText = buttonCopyLobbyId.GetComponentInChildren<TMP_Text>();
        dropdownMap.onValueChanged.AddListener(SubmitMap);
        dropdownMode.onValueChanged.AddListener(SubmitMode);
        dropdownGameEnd.onValueChanged.AddListener(SubmitEndChoice);

        UpdateGameEndOptions(dropdownMode.value);
    }

    private void OnEnable() {
        GameProgress.Instance.SelectedMap.OnValueChanged += MapChanged;
        TeamManager.Instance.CurrentMode.OnValueChanged += TeamModeChanged;
        TeamManager.Instance.EndChoice.OnValueChanged += GameEndChanged;
        UpdateReadyButton(LobbyManager.Instance.Me.IsReady());

        dropdownMode.ClearOptions();
        var freeForAll = R.String("gameMode.freeForAll");
        var teamDeathmatch = R.String("gameMode.teamDeathmatch");
        var captureTheFlag = R.String("gameMode.captureTheFlag");
        dropdownMode.AddOptions(new List<string> { freeForAll, teamDeathmatch, captureTheFlag });

        dropdownMap.options = GameMapDatabase.instance.gameMaps
            .Map(it => new TMP_Dropdown.OptionData(R.String($"map.{it.mapName}")))
            .ToList();
        UpdateGameEndTargetText();
    }

    private void MapChanged(int _, int newValue) {
        dropdownMap.captionText.text = dropdownMap.options[newValue].text;
    }

    private void TeamModeChanged(TeamManager.TeamMode _, TeamManager.TeamMode newValue) {
        dropdownMode.captionText.text = dropdownMode.options[(int)newValue].text;
    }

    private void GameEndChanged(int _, int newValue) {
        dropdownGameEnd.captionText.text = dropdownGameEnd.options[newValue].text;
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

        var showControls = LobbyManager.Instance.IsHost();
        _dropdownMapHelper.SetInteractable(showControls);
        _dropdownModeHelper.SetInteractable(showControls);
        _dropdownGameEndHelper.SetInteractable(showControls);
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
        UpdateGameEndTargetText();
    }

    private void SubmitEndChoice(int index) {
        TeamManager.Instance.SetEndChoice(index);
        UpdateGameEndTargetText();
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

    private void OnDisable() {
        GameProgress.Instance.SelectedMap.OnValueChanged -= MapChanged;
        TeamManager.Instance.CurrentMode.OnValueChanged -= TeamModeChanged;
        TeamManager.Instance.EndChoice.OnValueChanged -= GameEndChanged;
    }
}