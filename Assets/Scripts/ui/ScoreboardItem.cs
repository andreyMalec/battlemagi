using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardItem : MonoBehaviour {
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text flagsText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private RawImage colorImage;
    [SerializeField] private Image archetypeImage;
    [SerializeField] private Sprite[] spriteArchetypes;
    [SerializeField] private Shader colorShader;

    public void UpdateScore(PlayerManager.PlayerData data) {
        flagsText.text = data.Flags.ToString();
        killsText.text = data.Kills.ToString();
        deathsText.text = data.Deaths.ToString();
        assistsText.text = data.Assists.ToString();
        archetypeImage.overrideSprite = spriteArchetypes[data.Archetype];
    }

    public void UpdateName(string playerName, ulong steamId) {
        nameText.text = playerName;

        colorImage.material = new Material(colorShader);
        var color = new Friend(steamId).GetColor();
        colorImage.material.SetFloat(ColorizeMesh.Hue, color.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, color.saturation);

        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            flagsText.gameObject.SetActive(true);
            nameText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 310f);
        } else {
            flagsText.gameObject.SetActive(false);
            nameText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 460f);
        }
    }
}