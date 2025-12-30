using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LobbyMemberItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("References")]
    [SerializeField] private TMP_Text nameText;

    [SerializeField] private RawImage colorImage;
    [SerializeField] private Image readyImage;
    [SerializeField] private Image archetypeImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private GameObject sliderContainer;
    [SerializeField] private Image backgroundRaycast;
    public Transform root;

    [Header("Visuals")]
    [SerializeField] private Sprite spriteReady;

    [SerializeField] private Sprite spriteNotReady;
    [SerializeField] private Sprite[] spriteArchetypes;
    [SerializeField] private Shader colorShader;
    private ulong _steamId;

    void Awake() {
        if (backgroundRaycast != null)
            backgroundRaycast.raycastTarget = true;

        SetHover(false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        SetHover(false);
    }

    void SetHover(bool isHover) {
        nameText.gameObject.SetActive(!isHover);
        sliderContainer.SetActive(isHover);
    }

    private void OnEnable() {
        volumeSlider.onValueChanged.AddListener(VolumeChanged);
    }

    private void OnDisable() {
        volumeSlider.onValueChanged.RemoveAllListeners();
    }

    private void VolumeChanged(float value) {
        PlayersVoiceSettings.SetVolume(_steamId, value);
    }

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
        _steamId = steamId;
        volumeSlider.value = PlayersVoiceSettings.Volume(steamId);
        nameText.text = playerName;

        colorImage.material = new Material(colorShader);
        var color = new Friend(steamId).GetColor();
        colorImage.material.SetFloat(ColorizeMesh.Hue, color.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
    }
}