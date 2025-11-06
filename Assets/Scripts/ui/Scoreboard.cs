using System;
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
        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            flagText.gameObject.SetActive(true);
            nameText.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 380f);
        } else {
            flagText.gameObject.SetActive(false);
            nameText.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 530f);
        }

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