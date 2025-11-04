using System;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

public class LobbyMembers : MonoBehaviour {
    [SerializeField] private GameObject lobbyMemberPrefab;
    [SerializeField] private LayoutGroup containerTeamRed;
    [SerializeField] private LayoutGroup containerTeamBlue;
    [SerializeField] private Button buttonJoinRed;
    [SerializeField] private Button buttonJoinBlue;

    [Header("UI")]
    [SerializeField] private Sprite spriteReady;

    [SerializeField] private Sprite spriteNotReady;
    [SerializeField] private Shader colorShader;

    private Dictionary<ulong, MemberItem> lobbyMembers = new Dictionary<ulong, MemberItem>();

    private int frame = 0;

    private void OnEnable() {
        buttonJoinRed.onClick.AddListener(() => { LobbyExt.SetTeam(TeamManager.Team.Red); });
        buttonJoinBlue.onClick.AddListener(() => { LobbyExt.SetTeam(TeamManager.Team.Blue); });
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
        if (frame % 60 == 0) {
            foreach (var member in lobby.Value.Members) {
                var item = lobbyMembers[member.Id.Value];
                var image = member.IsReady() ? spriteReady : spriteNotReady;
                item.ready.overrideSprite = image;
                var color = member.GetColor();
                item.color.material.SetFloat(ColorizeMesh.Hue, color.hue);
                item.color.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
                UpdateTeam(member, item.root);
            }
        }

        var showTeams = TeamManager.Instance.isTeamMode;
        buttonJoinRed.gameObject.SetActive(showTeams);
        buttonJoinBlue.gameObject.SetActive(showTeams);
        containerTeamBlue.gameObject.transform.parent.gameObject.SetActive(showTeams);
    }

    private MemberItem Create(Friend friend) {
        var team = friend.GetTeam();
        var container = containerTeamRed.transform;
        if (team == TeamManager.Team.Blue)
            container = containerTeamBlue.transform;
        var item = Instantiate(lobbyMemberPrefab, container);
        item.transform.SetParent(container);
        var textName = item.GetComponentInChildren<TMP_Text>();
        textName.text = friend.Name;
        var imageColor = item.GetComponentInChildren<RawImage>();
        imageColor.material = new Material(colorShader);
        var imageReady = item.GetComponentInChildren<Image>();
        return new MemberItem(textName, imageColor, imageReady, item.transform);
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
            Destroy(item.name.transform.parent.gameObject);
            var tmp = new Dictionary<ulong, MemberItem>();
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

    private struct MemberItem {
        public TMP_Text name;
        public RawImage color;
        public Image ready;
        public Transform root;

        public MemberItem(TMP_Text name, RawImage color, Image ready, Transform root) {
            this.name = name;
            this.color = color;
            this.ready = ready;
            this.root = root;
        }
    }

    private void OnDisable() {
        buttonJoinRed.onClick.RemoveAllListeners();
        buttonJoinBlue.onClick.RemoveAllListeners();
    }
}