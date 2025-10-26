using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scoreboard : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.Tab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject container;

    private readonly Dictionary<ulong, ScoreboardItem> _items = new();

    private CanvasGroup _canvas;

    private void OnEnable() {
        var sorted = PlayerManager.Instance.Players()
            .OrderBy(it => TeamManager.Instance.GetTeam(it.ClientId));
        foreach (var player in sorted) {
            var item = Instantiate(itemPrefab, container.transform);
            var scoreboardItem = item.GetComponent<ScoreboardItem>();
            scoreboardItem.UpdateName(player.Name(), player.SteamId);
            scoreboardItem.UpdateScore(player);
            _items[player.ClientId] = scoreboardItem;
        }

        PlayerManager.Instance.OnListChanged += OnPlayersChanged;
    }

    private void OnDisable() {
        PlayerManager.Instance.OnListChanged -= OnPlayersChanged;
    }

    private void OnPlayersChanged(List<PlayerManager.PlayerData> players) {
        foreach (var player in players) {
            _items[player.ClientId].UpdateScore(player);
        }
    }

    private void Awake() {
        _canvas = GetComponent<CanvasGroup>();
    }

    private void Update() {
        _canvas.alpha = Input.GetKey(key) ? 1 : 0;
    }
}