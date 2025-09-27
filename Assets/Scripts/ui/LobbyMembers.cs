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
    [SerializeField] private LayoutGroup container;

    [Header("UI")]
    [SerializeField] private Sprite spriteReady;

    [SerializeField] private Sprite spriteNotReady;
    [SerializeField] private Shader colorShader;

    private Dictionary<ulong, MemberItem> lobbyMembers = new Dictionary<ulong, MemberItem>();

    private int frame = 0;

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
        if (frame % 60 == 0) {
            var lobby = LobbyManager.Instance.CurrentLobby;
            if (lobby.HasValue) {
                foreach (var member in lobby.Value.Members) {
                    var item = lobbyMembers[member.Id.Value];
                    var image = member.IsReady() ? spriteReady : spriteNotReady;
                    item.ready.overrideSprite = image;
                    var color = member.GetColor();
                    item.color.material.SetFloat(ColorizeMesh.Hue, color.hue);
                    item.color.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
                }
            }
        }
    }

    private MemberItem Create(Friend friend) {
        var item = Instantiate(lobbyMemberPrefab, container.transform);
        item.transform.SetParent(container.transform);
        var textName = item.GetComponentInChildren<TMP_Text>();
        textName.text = friend.Name;
        var imageColor = item.GetComponentInChildren<RawImage>();
        imageColor.material = new Material(colorShader);
        var imageReady = item.GetComponentInChildren<Image>();
        return new MemberItem(textName, imageColor, imageReady);
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

        public MemberItem(TMP_Text name, RawImage color, Image ready) {
            this.name = name;
            this.color = color;
            this.ready = ready;
        }
    }
}