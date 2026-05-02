using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Steamworks.Data;

public class MenuStateFindLobby : MonoBehaviour {
    [SerializeField] private Menu menu;
    [SerializeField] private Button buttonBack;
    [SerializeField] private Button buttonRefresh;
    [SerializeField] private Switch switchSortPlayers;
    [SerializeField] private TMP_Dropdown dropdownMap;
    [SerializeField] private TMP_Dropdown dropdownMode;
    [SerializeField] private Transform noItemsFound;
    [SerializeField] private Transform listRoot;
    [SerializeField] private LobbySearchItemView lobbyItemPrefab;

    private readonly List<LobbyEntry> _entries = new();
    private readonly List<LobbySearchItemView> _itemViews = new();

    private bool _refreshing = false;

    private void Awake() {
        buttonBack.onClick.AddListener(menu.BackToMain);
        buttonRefresh.onClick.AddListener(Refresh);
        switchSortPlayers.onClick.AddListener(RebuildList);
        dropdownMap.onValueChanged.AddListener(_ => RebuildList());
        dropdownMode.onValueChanged.AddListener(_ => RebuildList());
    }

    private void OnEnable() {
        SetupMapFilterOptions();
        SetupModeFilterOptions();
        LobbyManager.Instance.OnLobbyListUpdated += HandleLobbyListUpdated;
        Refresh();
    }

    private void OnDisable() {
        LobbyManager.Instance.OnLobbyListUpdated -= HandleLobbyListUpdated;
    }

    private void Refresh() {
        if (_refreshing) return;
        _refreshing = true;
        StartCoroutine(RefreshCoroutine());
    }

    private IEnumerator RefreshCoroutine() {
        LobbyManager.Instance.RefreshLobbyList();
        buttonRefresh.interactable = false;
        buttonRefresh.GetComponentInChildren<TMP_Text>().text = R.String("menuFind.refreshing");
        yield return new WaitForSeconds(3f);
        buttonRefresh.interactable = true;
        buttonRefresh.GetComponentInChildren<TMP_Text>().text = R.String("menuFind.refresh");
        _refreshing = false;
    }

    private void HandleLobbyListUpdated(IReadOnlyList<Lobby> lobbies) {
        _entries.Clear();

        foreach (var lobby in lobbies) {
            var mapIndex = ParseIntOrDefault(lobby.GetData(LobbyManager.KeyMap));
            var modeIndex = ParseIntOrDefault(lobby.GetData(LobbyManager.KeyMode));
            var host = lobby.GetData(LobbyManager.KeyHost);
            _entries.Add(new LobbyEntry {
                LobbyId = lobby.Id.Value,
                HostName = host,
                MapIndex = mapIndex,
                ModeIndex = modeIndex,
                Players = lobby.MemberCount,
                MaxPlayers = lobby.MaxMembers
            });
        }

        RebuildList();
    }

    private void RebuildList() {
        foreach (var view in _itemViews)
            Destroy(view.gameObject);
        _itemViews.Clear();

        var mapFilter = dropdownMap.value - 1;
        var modeFilter = dropdownMode.value - 1;

        IEnumerable<LobbyEntry> query = _entries;

        if (mapFilter >= 0)
            query = query.Where(it => it.MapIndex == mapFilter);

        if (modeFilter >= 0)
            query = query.Where(it => it.ModeIndex == modeFilter);

        query = switchSortPlayers.isChecked
            ? query.OrderByDescending(it => it.Players)
            : query.OrderBy(it => it.Players);

        foreach (var entry in query) {
            var item = Instantiate(lobbyItemPrefab, listRoot);
            item.Bind(entry.LobbyId, entry.HostName, GetMapName(entry.MapIndex), GetModeName(entry.ModeIndex),
                entry.Players, entry.MaxPlayers, JoinLobby);
            _itemViews.Add(item);
        }

        noItemsFound.gameObject.SetActive(_itemViews.Count == 0);
    }

    private void JoinLobby(ulong lobbyId) {
        LobbyManager.Instance.JoinLobby(lobbyId);
    }

    private void SetupMapFilterOptions() {
        dropdownMap.ClearOptions();
        var options = new List<string> { R.String("map.all") };
        foreach (var gameMap in GameMapDatabase.instance.gameMaps) {
            options.Add(R.String($"map.{gameMap.mapName}"));
        }

        dropdownMap.AddOptions(options);
        dropdownMap.value = 0;
    }

    private void SetupModeFilterOptions() {
        dropdownMode.ClearOptions();
        dropdownMode.AddOptions(new List<string> {
            R.String("gameMode.all"),
            GetModeName((int)TeamManager.TeamMode.FreeForAll),
            GetModeName((int)TeamManager.TeamMode.TwoTeams),
            GetModeName((int)TeamManager.TeamMode.CaptureTheFlag)
        });
        dropdownMode.value = 0;
    }

    private string GetMapName(int mapIndex) {
        var maps = GameMapDatabase.instance.gameMaps;
        if (mapIndex < 0 || mapIndex >= maps.Length)
            return "Unknown";

        return R.String($"map.{maps[mapIndex].mapName}");
    }

    private string GetModeName(int modeIndex) {
        if (modeIndex == (int)TeamManager.TeamMode.FreeForAll)
            return R.String("gameMode.short.freeForAll");

        if (modeIndex == (int)TeamManager.TeamMode.TwoTeams)
            return R.String("gameMode.short.teamDeathmatch");

        if (modeIndex == (int)TeamManager.TeamMode.CaptureTheFlag)
            return R.String("gameMode.short.captureTheFlag");

        return "Unknown";
    }

    private int ParseIntOrDefault(string value) {
        return int.TryParse(value, out var number) ? number : 0;
    }

    private struct LobbyEntry {
        public ulong LobbyId;
        public string HostName;
        public int MapIndex;
        public int ModeIndex;
        public int Players;
        public int MaxPlayers;
    }
}