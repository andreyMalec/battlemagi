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
    private LobbyMemberItem _addBotItem;
    private string _lastBotRosterRaw;

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

        EnsureAddBotItem();
    }

    private void Update() {
        frame++;
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue) return;
        if (frame % 5 == 0) {
            foreach (var member in lobby.Value.Members) {
                if (!lobbyMembers.TryGetValue(member.Id.Value, out var item)) continue;
                item.UpdateState(member);
                UpdateTeam(member, item.root);
            }

            SyncBots(lobby.Value, false);
            EnsureAddBotItem();
        }

        var showTeams = TeamManager.Instance.isTeamMode;
        buttonJoinRed.gameObject.SetActive(showTeams);
        buttonJoinBlue.gameObject.SetActive(showTeams);
        containerTeamBlue.gameObject.transform.parent.gameObject.SetActive(showTeams);
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
        var go = Instantiate(lobbyMemberPrefab, containerTeamRed.transform);
        go.transform.SetParent(containerTeamRed.transform);
        var item = go.GetComponent<LobbyMemberItem>();
        item.UpdateBotState(
            bot,
            RemoveBot,
            SetBotArchetype
        );
        return item;
    }

    private void EnsureAddBotItem() {
        var isHost = LobbyManager.Instance.IsHost();
        if (!isHost) {
            if (_addBotItem != null)
                Destroy(_addBotItem.root.gameObject);
            _addBotItem = null;
            return;
        }

        var maxMembers = LobbyManager.Instance.CurrentLobby?.MaxMembers ?? 0;
        if (lobbyMembers.Count + botMembers.Count >= maxMembers) {
            if (_addBotItem != null)
                Destroy(_addBotItem.root.gameObject);
            return;
        }

        if (_addBotItem != null)
            return;

        var go = Instantiate(lobbyMemberPrefab, containerTeamRed.transform);
        go.transform.SetParent(containerTeamRed.transform);
        _addBotItem = go.GetComponent<LobbyMemberItem>();
        _addBotItem.UpdateAddBotState(AddBot);
    }

    private void AddBot() {
        if (!LobbyManager.Instance.IsHost())
            return;

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue)
            return;

        if (_addBotItem != null)
            Destroy(_addBotItem.root.gameObject);

        var bots = LobbyBotRosterData.LoadFromLobby(lobby.Value);
        var bot = new LobbyBotRosterData.Entry {
            id = LobbyBotRosterData.NextId(bots),
            archetype = Random.Range(0, 4),
            hue = Random.Range(0f, 360f),
            saturation = Random.Range(0.4f, 1f)
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
        EnsureAddBotItem();
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend) {
        Destroy(friend.Id.Value);
        EnsureAddBotItem();
    }

    private void OnDisable() {
        buttonJoinRed.onClick.RemoveListener(JoinRed);
        buttonJoinBlue.onClick.RemoveListener(JoinBlue);
    }
}