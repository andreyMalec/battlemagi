using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;

public class PlayersVoiceSettings : MonoBehaviour {
    [SerializeField] private GameObject playerVoicePrefab;
    [SerializeField] private CanvasGroup container;

    private Dictionary<ulong, GameObject> lobbyMembers = new();

    private void OnEnable() {
        RequestUpdate();
    }

    public void RequestUpdate() {
        foreach (var member in lobbyMembers.Keys) {
            Destroy(member);
        }

        lobbyMembers.Clear();
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby.HasValue) {
            foreach (var member in lobby.Value.Members) {
                if (member.IsMe) continue;
                lobbyMembers[member.Id.Value] = Create(member);
            }
        }
        container.alpha = lobbyMembers.Count == 0 ? 0 : 1;
    }

    private GameObject Create(Friend friend) {
        var item = Instantiate(playerVoicePrefab, container.transform).GetComponent<PlayerVoiceItem>();
        var textName = item.GetComponentInChildren<TMP_Text>();
        textName.text = friend.Name;
        var steamId = friend.Id.Value;
        item.UpdateItem(steamId, friend.Name, Volume(steamId));
        return item.gameObject;
    }

    private void Destroy(ulong steamId) {
        if (lobbyMembers.TryGetValue(steamId, out var item)) {
            Destroy(item.transform.gameObject);
            var tmp = new Dictionary<ulong, GameObject>();
            foreach (var member in lobbyMembers.Keys) {
                if (member == steamId) continue;
                tmp[member] = lobbyMembers[member];
            }

            lobbyMembers = tmp;
        }
    }

    public static float Volume(ulong steamId) {
        return PlayerPrefs.GetFloat($"volume_settings_{steamId}", 1);
    }

    public static void SetVolume(ulong steamId, float value) {
        PlayerPrefs.SetFloat($"volume_settings_{steamId}", value);
    }
}