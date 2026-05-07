using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardItem : MonoBehaviour {
    private const float PacketLossWarningThreshold = 5f;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text flagsText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private RawImage colorImage;
    [SerializeField] private Image archetypeImage;
    [SerializeField] private Sprite[] spriteArchetypes;
    [SerializeField] private Shader colorShader;

    public void UpdateScore(MatchParticipantData data) {
        flagsText.text = data.Flags.ToString();
        killsText.text = data.Kills.ToString();
        deathsText.text = data.Deaths.ToString();
        assistsText.text = data.Assists.ToString();
        if (data.Id.IsBot) {
            pingText.text = "-";
        } else {
            if (PlayerManager.Instance.TryGetPlayerData(data.Id.Value, out var playerData)) {
                pingText.text = playerData.PacketLossPercent > PacketLossWarningThreshold
                    ? $"{playerData.PingMs} ms <color=#FFB54A><b>!</b></color>"
                    : $"{playerData.PingMs} ms";
            } else {
                pingText.text = "-";
            }
        }

        if (data.Archetype >= 0 && data.Archetype < spriteArchetypes.Length)
            archetypeImage.overrideSprite = spriteArchetypes[data.Archetype];
        else
            archetypeImage.overrideSprite = null;
    }

    public void UpdateName(string playerName, float hue, float saturation) {
        nameText.text = playerName;

        colorImage.material = new Material(colorShader);
        colorImage.material.SetFloat(ColorizeMesh.Hue, hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, saturation);

        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            flagsText.gameObject.SetActive(true);
            nameText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 310f);
        } else {
            flagsText.gameObject.SetActive(false);
            nameText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 460f);
        }
    }
}