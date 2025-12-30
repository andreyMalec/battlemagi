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
        foreach (var member in lobbyMembers.Keys) {
            Destroy(member);
        }

        lobbyMembers.Clear();
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby.HasValue) {
            foreach (var member in lobby.Value.Members) {
                lobbyMembers[member.Id.Value] = Create(member);
            }
        }
    }

    private void Update() {
        frame++;
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (!lobby.HasValue) return;
        if (frame % 5 == 0) {
            foreach (var member in lobby.Value.Members) {
                var item = lobbyMembers[member.Id.Value];

                item.UpdateState(member);
                UpdateTeam(member, item.root);
            }
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
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend) {
        Destroy(friend.Id.Value);
    }

    private void OnDisable() {
        buttonJoinRed.onClick.RemoveListener(JoinRed);
        buttonJoinBlue.onClick.RemoveListener(JoinBlue);
    }
}