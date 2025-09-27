using TMPro;
using UnityEngine;

public class ScoreboardItem : MonoBehaviour {
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text assistsText;

    public void UpdateScore(PlayerManager.PlayerData data) {
        killsText.text = data.Kills.ToString();
        deathsText.text = data.Deaths.ToString();
        assistsText.text = data.Assists.ToString();
    }

    public void UpdateName(string playerName) {
        nameText.text = playerName;
    }
}