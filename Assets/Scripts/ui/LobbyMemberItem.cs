using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberItem : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TMP_Text nameText;

    [SerializeField] private RawImage colorImage;
    [SerializeField] private Image readyImage;
    [SerializeField] private Image archetypeImage;
    public Transform root;

    [Header("Visuals")]
    [SerializeField] private Sprite spriteReady;

    [SerializeField] private Sprite spriteNotReady;
    [SerializeField] private Sprite[] spriteArchetypes;
    [SerializeField] private Shader colorShader;

    public void UpdateState(Friend member) {
        var image = member.IsReady() ? spriteReady : spriteNotReady;
        readyImage.overrideSprite = image;
        var color = member.GetColor();
        colorImage.material.SetFloat(ColorizeMesh.Hue, color.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
        var d = PlayerManager.Instance.FindBySteamId(member.Id.Value);
        if (d.HasValue && d.Value.Archetype >= 0)
            archetypeImage.overrideSprite = spriteArchetypes[d.Value.Archetype];
        else
            archetypeImage.overrideSprite = null;
    }

    public void UpdateName(string playerName, ulong steamId) {
        nameText.text = playerName;

        colorImage.material = new Material(colorShader);
        var color = new Friend(steamId).GetColor();
        colorImage.material.SetFloat(ColorizeMesh.Hue, color.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
    }
}