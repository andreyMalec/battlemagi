using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySearchItemView : MonoBehaviour {
    [SerializeField] private TMP_Text hostName;
    [SerializeField] private TMP_Text mapName;
    [SerializeField] private TMP_Text modeName;
    [SerializeField] private TMP_Text playersCount;
    [SerializeField] private Button buttonJoin;

    public void Bind(ulong lobbyId, string host, string map, string mode, int players, int maxPlayers, Action<ulong> onJoin) {
        hostName.text = host;
        mapName.text = map;
        modeName.text = mode;
        playersCount.text = $"{players}/{maxPlayers}";

        buttonJoin.onClick.RemoveAllListeners();
        buttonJoin.onClick.AddListener(() => onJoin(lobbyId));
    }
}

