using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Scoreboard : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.Tab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject flagText;
    [SerializeField] private RectTransform nameText;

    private readonly Dictionary<ParticipantId, ScoreboardItem> _items = new();

    private CanvasGroup _canvas;
    private int _refreshFrame;

    private void OnEnable() {
        if (TeamManager.Instance == null) return;

        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            flagText.gameObject.SetActive(true);
            nameText.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 380f);
        } else {
            flagText.gameObject.SetActive(false);
            nameText.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 530f);
        }

        ClearItems();
        RefreshItems(PlayerManager.Instance.Participants);

        PlayerManager.Instance.OnListChanged += OnPlayersChanged;
    }

    private void OnDisable() {
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnListChanged -= OnPlayersChanged;
        }
        ClearItems();
    }

    private void OnPlayersChanged(NetworkList<MatchParticipantData> participants) {
        _ = participants;
        RefreshItems(PlayerManager.Instance.Participants);
    }

    private void RefreshItems(IReadOnlyList<MatchParticipantData> participants) {
        var alive = new HashSet<ParticipantId>();
        var sorted = participants.OrderBy(it => TeamManager.Instance.GetTeam(it.Id));
        foreach (var participant in sorted) {
            alive.Add(participant.Id);
            if (!_items.TryGetValue(participant.Id, out var scoreboardItem)) {
                var item = Instantiate(itemPrefab, container.transform);
                scoreboardItem = item.GetComponent<ScoreboardItem>();
                _items[participant.Id] = scoreboardItem;
            }

            scoreboardItem.UpdateName(ResolveParticipantName(participant), participant.Hue, participant.Saturation);
            scoreboardItem.UpdateScore(participant);
        }

        var removed = _items.Keys.Where(id => !alive.Contains(id)).ToList();
        foreach (var id in removed) {
            Destroy(_items[id].gameObject);
            _items.Remove(id);
        }
    }

    private static string ResolveParticipantName(MatchParticipantData participant) {
        if (participant.Id.IsBot)
            return $"Bot #{participant.Id.Value}";

        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby.HasValue) {
            foreach (var member in lobby.Value.Members) {
                if (member.Id.Value == participant.SteamId)
                    return member.Name;
            }
        }

        return $"Player_{participant.Id.Value}";
    }

    private void ClearItems() {
        foreach (var item in _items.Values) {
            Destroy(item.gameObject);
        }

        _items.Clear();
    }

    private void Awake() {
        _canvas = GetComponent<CanvasGroup>();
    }

    private void Update() {
        _canvas.alpha = Input.GetKey(key) ? 1 : 0;
        _refreshFrame++;
        if (_refreshFrame % 5 == 0 && PlayerManager.Instance != null)
            RefreshItems(PlayerManager.Instance.Participants);
    }
}