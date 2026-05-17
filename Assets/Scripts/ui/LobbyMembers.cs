using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMembers : MonoBehaviour {
    [SerializeField] private GameObject lobbyMemberPrefab;
    [SerializeField] private LayoutGroup containerTeamRed;
    [SerializeField] private LayoutGroup containerTeamBlue;
    [SerializeField] private Button buttonJoinRed;
    [SerializeField] private Button buttonJoinBlue;

    private Dictionary<ulong, LobbyMemberItem> lobbyMembers = new Dictionary<ulong, LobbyMemberItem>();
    private Dictionary<ulong, LobbyMemberItem> botMembers = new Dictionary<ulong, LobbyMemberItem>();
    private LobbyMemberItem _addBotItemRed;
    private LobbyMemberItem _addBotItemBlue;
    private string _lastBotRosterRaw;
    private bool? _lastIsTeamMode;

    private int frame = 0;

    private void OnEnable() {
        buttonJoinRed.onClick.AddListener(JoinRed);
        buttonJoinBlue.onClick.AddListener(JoinBlue);
    }

    private void JoinRed() {
        LobbyExt.SetTeam(TeamManager.Team.Red);
    }

    private void JoinBlue() {
        LobbyExt.SetTeam(TeamManager.Team.Blue);
    }

    public void RequestUpdate() {
        var humanIds = new List<ulong>(lobbyMembers.Keys);
        for (int i = 0; i < humanIds.Count; i++) {
            Destroy(humanIds[i]);
        }

        ClearBotItems();
        lobbyMembers.Clear();
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby.HasValue) {
            foreach (var member in lobby.Value.Members) {
                lobbyMembers[member.Id.Value] = Create(member);
            }

            SyncBots(lobby.Value, true);
        }

        EnsureAddBotItem(true);
        KeepAddBotItemsAtEnd();
        _lastIsTeamMode = TeamManager.Instance.isTeamMode;
    }

    private void Update() {
        frame++;
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue) return;

        var showTeams = TeamManager.Instance.isTeamMode;
        var modeChanged = _lastIsTeamMode.HasValue && _lastIsTeamMode.Value != showTeams;
        if (modeChanged) {
            SyncBots(lobby.Value, true);
            EnsureAddBotItem(true);
            KeepAddBotItemsAtEnd();
        }

        if (frame % 5 == 0) {
            foreach (var member in lobby.Value.Members) {
                if (!lobbyMembers.TryGetValue(member.Id.Value, out var item)) continue;
                item.UpdateState(member);
                UpdateTeam(member, item.root);
            }

            SyncBots(lobby.Value, false);
            EnsureAddBotItem(false);
            KeepAddBotItemsAtEnd();
        }

        buttonJoinRed.gameObject.SetActive(showTeams);
        buttonJoinBlue.gameObject.SetActive(showTeams);
        containerTeamBlue.gameObject.transform.parent.gameObject.SetActive(showTeams);
        _lastIsTeamMode = showTeams;
    }

    private LobbyMemberItem Create(Friend friend) {
        var team = friend.GetTeam();
        var container = containerTeamRed.transform;
        if (team == TeamManager.Team.Blue)
            container = containerTeamBlue.transform;
        var go = Instantiate(lobbyMemberPrefab, container);
        go.transform.SetParent(container);
        var item = go.GetComponent<LobbyMemberItem>();
        item.UpdateName(friend.Name, friend.Id);
        return item;
    }

    private LobbyMemberItem CreateBot(LobbyBotRosterData.Entry bot) {
        var container = GetBotContainer(bot.team);
        var go = Instantiate(lobbyMemberPrefab, container);
        go.transform.SetParent(container);
        var item = go.GetComponent<LobbyMemberItem>();
        item.UpdateBotState(
            bot,
            RemoveBot,
            SetBotArchetype
        );
        return item;
    }

    private void EnsureAddBotItem(bool forceRemove) {
        var isHost = LobbyManager.Instance.IsHost();
        var isTeamMode = TeamManager.Instance.isTeamMode;
        if (!isHost) {
            DestroyAddBotItems();
            return;
        }

        var maxMembers = LobbyManager.Instance.CurrentLobby?.MaxMembers ?? 0;
        if (forceRemove || lobbyMembers.Count + botMembers.Count >= maxMembers) {
            DestroyAddBotItems();
            if (!forceRemove)
                return;
        }

        if (isTeamMode) {
            EnsureAddBotItemForTeam(TeamManager.Team.Red);
            EnsureAddBotItemForTeam(TeamManager.Team.Blue);
            return;
        }

        if (_addBotItemRed != null)
            return;

        var go = Instantiate(lobbyMemberPrefab, containerTeamRed.transform);
        go.transform.SetParent(containerTeamRed.transform);
        _addBotItemRed = go.GetComponent<LobbyMemberItem>();
        _addBotItemRed.UpdateAddBotState(() => AddBot(TeamManager.Team.None));
    }

    private void EnsureAddBotItemForTeam(TeamManager.Team team) {
        var current = team == TeamManager.Team.Blue ? _addBotItemBlue : _addBotItemRed;
        if (current != null)
            return;

        var container = GetBotContainer(team);
        var go = Instantiate(lobbyMemberPrefab, container);
        go.transform.SetParent(container);
        var item = go.GetComponent<LobbyMemberItem>();
        item.UpdateAddBotState(() => AddBot(team));

        if (team == TeamManager.Team.Blue)
            _addBotItemBlue = item;
        else
            _addBotItemRed = item;

        KeepAddBotItemsAtEnd();
    }

    private void DestroyAddBotItems() {
        if (_addBotItemRed != null)
            Destroy(_addBotItemRed.root.gameObject);
        if (_addBotItemBlue != null)
            Destroy(_addBotItemBlue.root.gameObject);
        _addBotItemRed = null;
        _addBotItemBlue = null;
    }

    private void AddBot(TeamManager.Team team) {
        if (!LobbyManager.Instance.IsHost())
            return;

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue)
            return;

        DestroyAddBotItems();

        var hue = Random.Range(0f, 360f);
        var saturation = Random.Range(0.4f, 1f);
        if (TeamManager.Instance.isTeamMode) {
            if (team == TeamManager.Team.Blue) {
                hue = 228f;
                saturation = 0.85f;
            } else {
                hue = 0f;
                saturation = 0.85f;
            }
        }

        var bots = LobbyBotRosterData.LoadFromLobby(lobby.Value);
        var bot = new LobbyBotRosterData.Entry {
            id = LobbyBotRosterData.NextId(bots),
            team = team,
            archetype = Random.Range(0, 4),
            hue = hue,
            saturation = saturation
        };
        bots.Add(bot);
        LobbyBotRosterData.SaveToLobby(lobby.Value, bots);
        SyncBots(lobby.Value, true);
    }

    private void RemoveBot(ulong botId) {
        if (!LobbyManager.Instance.IsHost())
            return;

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue)
            return;

        var bots = LobbyBotRosterData.LoadFromLobby(lobby.Value);
        for (int i = bots.Count - 1; i >= 0; i--) {
            if (bots[i].id == botId)
                bots.RemoveAt(i);
        }

        LobbyBotRosterData.SaveToLobby(lobby.Value, bots);
        SyncBots(lobby.Value, true);
        EnsureAddBotItem(true);
    }

    private void SetBotArchetype(ulong botId, int archetype) {
        if (!LobbyManager.Instance.IsHost())
            return;

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue)
            return;

        var bots = LobbyBotRosterData.LoadFromLobby(lobby.Value);
        for (int i = 0; i < bots.Count; i++) {
            if (bots[i].id != botId)
                continue;
            bots[i].archetype = archetype;
            break;
        }

        LobbyBotRosterData.SaveToLobby(lobby.Value, bots);
        SyncBots(lobby.Value, true);
    }

    private void SyncBots(Lobby lobby, bool force) {
        var raw = lobby.GetData(LobbyBotRosterData.LobbyDataKey);
        if (!force && raw == _lastBotRosterRaw)
            return;
        _lastBotRosterRaw = raw;

        var bots = LobbyBotRosterData.LoadFromLobby(lobby);
        var keep = new HashSet<ulong>();
        for (int i = 0; i < bots.Count; i++) {
            var bot = bots[i];
            keep.Add(bot.id);
            if (!botMembers.TryGetValue(bot.id, out var item)) {
                item = CreateBot(bot);
                botMembers[bot.id] = item;
            }

            item.UpdateBotState(
                bot,
                RemoveBot,
                SetBotArchetype
            );
            var container = GetBotContainer(bot.team);
            if (item.root.parent != container)
                item.root.SetParent(container, false);
        }

        var existing = new List<ulong>(botMembers.Keys);
        for (int i = 0; i < existing.Count; i++) {
            var id = existing[i];
            if (keep.Contains(id))
                continue;
            if (botMembers.TryGetValue(id, out var item))
                Destroy(item.root.gameObject);
            botMembers.Remove(id);
        }

        KeepAddBotItemsAtEnd();
    }

    private void ClearBotItems() {
        var ids = new List<ulong>(botMembers.Keys);
        for (int i = 0; i < ids.Count; i++) {
            if (botMembers.TryGetValue(ids[i], out var item))
                Destroy(item.root.gameObject);
        }

        botMembers.Clear();
    }

    private void UpdateTeam(Friend friend, Transform item) {
        var team = friend.GetTeam();
        var container = containerTeamRed.transform;
        if (TeamManager.Instance.isTeamMode && team == TeamManager.Team.Blue)
            container = containerTeamBlue.transform;
        item.SetParent(container, false);
    }

    private Transform GetBotContainer(TeamManager.Team team) {
        if (TeamManager.Instance.isTeamMode && team == TeamManager.Team.Blue)
            return containerTeamBlue.transform;
        return containerTeamRed.transform;
    }

    private void KeepAddBotItemsAtEnd() {
        if (_addBotItemRed != null)
            _addBotItemRed.root.SetAsLastSibling();
        if (_addBotItemBlue != null)
            _addBotItemBlue.root.SetAsLastSibling();
    }

    private void Destroy(ulong steamId) {
        if (lobbyMembers.TryGetValue(steamId, out var item)) {
            Destroy(item.root.gameObject);
            var tmp = new Dictionary<ulong, LobbyMemberItem>();
            foreach (var member in lobbyMembers.Keys) {
                if (member == steamId) continue;
                tmp[member] = lobbyMembers[member];
            }

            lobbyMembers = tmp;
        }
    }

    private void Awake() {
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
    }

    private void OnDestroy() {
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend) {
        lobbyMembers[friend.Id.Value] = Create(friend);
        EnsureAddBotItem(true);
        KeepAddBotItemsAtEnd();
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend) {
        Destroy(friend.Id.Value);
        EnsureAddBotItem(true);
        KeepAddBotItemsAtEnd();
    }

    private void OnDisable() {
        buttonJoinRed.onClick.RemoveListener(JoinRed);
        buttonJoinBlue.onClick.RemoveListener(JoinBlue);
    }
}