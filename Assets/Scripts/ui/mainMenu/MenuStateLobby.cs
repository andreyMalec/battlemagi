using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MenuStateLobby : MonoBehaviour {
    [SerializeField] private Button buttonInvite;
    [SerializeField] private Button buttonBackToMain;
    [SerializeField] private Button buttonCopyLobbyId;
    [SerializeField] private Button buttonReady;
    [SerializeField] private TMP_Dropdown dropdownLobbyVisibility;
    [SerializeField] private TMP_Dropdown dropdownMap;
    [SerializeField] private TMP_Dropdown dropdownMode;
    [SerializeField] private TMP_Dropdown dropdownGameEnd;
    [SerializeField] private TMP_Dropdown dropdownKeyCast;
    [SerializeField] private TMP_Text gameEndTarget;
    [SerializeField] private TMP_Text lobbyName;
    [SerializeField] private TMP_InputField fieldLobbyId;
    private TMP_Text copyButtonText;

    private const string PrefLobbyVisibility = "menu.lobby.dropdown.visibility";
    private const string PrefMap = "menu.lobby.dropdown.map";
    private const string PrefMode = "menu.lobby.dropdown.mode";
    private const string PrefGameEnd = "menu.lobby.dropdown.gameEnd";
    private const string PrefKeyCast = "menu.lobby.dropdown.keyCast";

    private int readyCount = 0;

    private DropdownHelper _dropdownLobbyVisibilityHelper;
    private DropdownHelper _dropdownMapHelper;
    private DropdownHelper _dropdownModeHelper;
    private DropdownHelper _dropdownGameEndHelper;
    private DropdownHelper _dropdownKeyCastHelper;

    private bool _ignoreMapChange;

    private void Awake() {
        _dropdownLobbyVisibilityHelper = dropdownLobbyVisibility.GetComponent<DropdownHelper>();
        _dropdownMapHelper = dropdownMap.GetComponent<DropdownHelper>();
        _dropdownModeHelper = dropdownMode.GetComponent<DropdownHelper>();
        _dropdownGameEndHelper = dropdownGameEnd.GetComponent<DropdownHelper>();
        _dropdownKeyCastHelper = dropdownKeyCast.GetComponent<DropdownHelper>();

        buttonBackToMain.onClick.AddListener(LeaveLobby);
        buttonInvite.onClick.AddListener(InviteFriends);
        buttonReady.onClick.AddListener(ToggleReady);
        buttonCopyLobbyId.onClick.AddListener(() => StartCoroutine(CopyId()));
        copyButtonText = buttonCopyLobbyId.GetComponentInChildren<TMP_Text>();
        dropdownLobbyVisibility.onValueChanged.AddListener(SubmitVisibility);
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
        var chargedShotOnly = R.String("gameMode.chargedShotOnly");
        dropdownMode.AddOptions(new List<string> { freeForAll, teamDeathmatch, captureTheFlag, chargedShotOnly });

        dropdownLobbyVisibility.options = new List<TMP_Dropdown.OptionData>() {
            new(R.String("menuLobby.public")),
            new(R.String("menuLobby.friends")),
            new(R.String("menuLobby.private")),
        };
        dropdownLobbyVisibility.value = (int)LobbyManager.Instance.GetVisibility();
        dropdownMap.options = BuildMapOptions(dropdownMode.value);
        dropdownKeyCast.options = new List<TMP_Dropdown.OptionData>() {
            new(R.String("lobby.keyCast.disabled")),
            new(R.String("lobby.keyCast.enabled")),
        };

        StartCoroutine(LoadDropdownState());

        UpdateGameEndTargetText();
        PublishLobbyMeta();
    }

    private void OnDisable() {
        TeamManager.Instance.CurrentMode.OnValueChanged -= OnGameModeChanged;
    }

    private List<TMP_Dropdown.OptionData> BuildMapOptions(int modeIndex) {
        var isCtf = modeIndex == (int)TeamManager.TeamMode.CaptureTheFlag;
        return GameMapDatabase.instance.gameMaps
            .Map(it => {
                var text = R.String($"map.{it.mapName}");
                var option = new TMP_Dropdown.OptionData(text);
                if (isCtf && !it.activeCTF)
                    option.text = $"~{text}~";
                return option;
            })
            .ToList();
    }

    private void UpdateVisibility() {
        dropdownLobbyVisibility.captionText.text =
            dropdownLobbyVisibility.options[GameProgress.Instance.LobbyVisibility.Value].text;
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
        _dropdownLobbyVisibilityHelper.SetInteractable(showControls);
        _dropdownMapHelper.SetInteractable(showControls);
        _dropdownModeHelper.SetInteractable(showControls);
        _dropdownGameEndHelper.SetInteractable(showControls);
        _dropdownKeyCastHelper.SetInteractable(showControls);
        UpdateGameEndTargetText();
        UpdateVisibility();
        UpdateMap();
        UpdateTeamMode();
        UpdateGameEnd();
    }

    private void StartGame() {
        GameProgress.Instance.StartMatch();
    }

    private void SubmitVisibility(int index) {
        LobbyManager.Instance.SetVisibility((LobbyVisibility)index);
        SaveDropdownValue(PrefLobbyVisibility, index);
        UpdateVisibility();
    }

    private void SubmitMap(int index) {
        if (_ignoreMapChange) return;

        if (dropdownMode.value == (int)TeamManager.TeamMode.CaptureTheFlag) {
            if (!GameMapDatabase.instance.gameMaps[index].activeCTF) {
                _ignoreMapChange = true;
                dropdownMap.value = GameProgress.Instance.SelectedMap.Value;
                _ignoreMapChange = false;
                return;
            }
        }

        GameProgress.Instance.SelectMap(index);
        SaveDropdownValue(PrefMap, index);
        PublishLobbyMeta();
    }

    private void SubmitMode(int index) {
        TeamManager.Instance.SetMode((TeamManager.TeamMode)index);
        UpdateGameEndOptions(index);

        dropdownMap.options = BuildMapOptions(index);

        if (index == (int)TeamManager.TeamMode.CaptureTheFlag) {
            var current = GameProgress.Instance.SelectedMap.Value;
            var maps = GameMapDatabase.instance.gameMaps;
            if (!maps[current].activeCTF) {
                var next = Array.FindIndex(maps, m => m.activeCTF);
                if (next >= 0) {
                    GameProgress.Instance.SelectMap(next);
                    _ignoreMapChange = true;
                    dropdownMap.value = next;
                    _ignoreMapChange = false;
                }
            }
        }

        SaveDropdownValue(PrefMode, index);
        UpdateMap();
        PublishLobbyMeta();
    }

    private void SubmitEndChoice(int index) {
        TeamManager.Instance.SetEndChoice(index);
        SaveDropdownValue(PrefGameEnd, index);
    }

    private void SubmitKeyCast(int index) {
        GameProgress.Instance.SetKeyCast(index == 1);
        SaveDropdownValue(PrefKeyCast, index);
    }

    private void OnGameModeChanged(TeamManager.TeamMode o1, TeamManager.TeamMode o2) {
        UpdateGameEndOptions((int)o2);
        Debug.Log($"Client: OnGameModeChanged {o1} -> {o2}");
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
    }

    private void UpdateGameEndTargetText() {
        gameEndTarget.text = TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag
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
    }

    private IEnumerator LoadDropdownState() {
        while (!NetworkManager.Singleton.IsConnectedClient) {
            yield return null;
        }

        TeamManager.Instance.CurrentMode.OnValueChanged += OnGameModeChanged;
        UpdateGameEndOptions((int)TeamManager.Instance.CurrentMode.Value);

        if (!NetworkManager.Singleton.IsHost) yield break;

        var visibilityIndex = GetSavedDropdownValue(PrefLobbyVisibility, dropdownLobbyVisibility.value,
            dropdownLobbyVisibility.options.Count);
        dropdownLobbyVisibility.SetValueWithoutNotify(visibilityIndex);
        SubmitVisibility(visibilityIndex);

        var keyCastIndex = GetSavedDropdownValue(PrefKeyCast, GameConfig.Instance.allowKeySpells ? 1 : 0,
            dropdownKeyCast.options.Count);
        dropdownKeyCast.SetValueWithoutNotify(keyCastIndex);
        SubmitKeyCast(keyCastIndex);

        var mapIndex =
            GetSavedDropdownValue(PrefMap, GameProgress.Instance.SelectedMap.Value, dropdownMap.options.Count);
        dropdownMap.SetValueWithoutNotify(mapIndex);
        SubmitMap(mapIndex);

        while (LobbyManager.Instance.CurrentLobby == null
               || LobbyManager.Instance.CurrentLobby.Value.MemberCount == 0) {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        var modeIndex = GetSavedDropdownValue(PrefMode, dropdownMode.value, dropdownMode.options.Count);
        dropdownMode.SetValueWithoutNotify(modeIndex);
        SubmitMode(modeIndex);

        var gameEndIndex = GetSavedDropdownValue(PrefGameEnd, dropdownGameEnd.value, dropdownGameEnd.options.Count);
        dropdownGameEnd.SetValueWithoutNotify(gameEndIndex);
        SubmitEndChoice(gameEndIndex);
    }

    private static int GetSavedDropdownValue(string key, int fallbackValue, int optionsCount) {
        var value = PlayerPrefs.GetInt(key, fallbackValue);
        return Mathf.Clamp(value, 0, optionsCount - 1);
    }

    private void SaveDropdownValue(string key, int value) {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    private void PublishLobbyMeta() {
        LobbyManager.Instance.UpdateLobbyMeta(GameProgress.Instance.SelectedMap.Value,
            TeamManager.Instance.CurrentMode.Value);
    }

    private IEnumerator CopyId() {
        GUIUtility.systemCopyBuffer = LobbyManager.Instance.CurrentLobby?.Id.ToString();
        copyButtonText.text = "OK";
        yield return new WaitForSeconds(1);
        copyButtonText.text = "Copy";
    }
}

public enum LobbyVisibility {
    Public,
    Friends,
    Private
}