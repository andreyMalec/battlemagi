using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scoreboard : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.Tab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject flagText;
    [SerializeField] private RectTransform nameText;

    private readonly Dictionary<ulong, ScoreboardItem> _items = new();

    private CanvasGroup _canvas;

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
        RefreshItems(PlayerManager.Instance.Players());

        PlayerManager.Instance.OnListChanged += OnPlayersChanged;
    }

    private void OnDisable() {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnListChanged -= OnPlayersChanged;
        ClearItems();
    }

    private void OnPlayersChanged(List<PlayerManager.PlayerData> players) {
        RefreshItems(players);
    }

    private void RefreshItems(List<PlayerManager.PlayerData> players) {
        var alive = new HashSet<ulong>();
        var sorted = players.OrderBy(it => TeamManager.Instance.GetTeam(it.ClientId));
        foreach (var player in sorted) {
            alive.Add(player.SteamId);
            if (!_items.TryGetValue(player.SteamId, out var scoreboardItem)) {
                var item = Instantiate(itemPrefab, container.transform);
                scoreboardItem = item.GetComponent<ScoreboardItem>();
                _items[player.SteamId] = scoreboardItem;
            }

            scoreboardItem.UpdateName(player.Name(), player.SteamId);
            scoreboardItem.UpdateScore(player);
        }

        var removed = _items.Keys.Where(steamId => !alive.Contains(steamId)).ToList();
        foreach (var steamId in removed) {
            Destroy(_items[steamId].gameObject);
            _items.Remove(steamId);
        }
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
    }
}