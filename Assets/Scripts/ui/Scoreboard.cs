using System;
using System.Collections.Generic;
using UnityEngine;

public class Scoreboard : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.Tab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject container;

    private readonly Dictionary<ulong, ScoreboardItem> _items = new();
    
    private Canvas _canvas;

    private void OnEnable() {
        foreach (var player in PlayerManager.Instance.Players()) {
            var item = Instantiate(itemPrefab, container.transform);
            var scoreboardItem = item.GetComponent<ScoreboardItem>();
            scoreboardItem.UpdateName(player.Name());
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
        _canvas = GetComponent<Canvas>();
    }

    private void Update() {
        _canvas.enabled = Input.GetKey(key);
    }
}