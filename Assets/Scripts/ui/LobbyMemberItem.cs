using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class LobbyMemberItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("References")]
    [SerializeField] private GameObject stateItem;

    [SerializeField] private GameObject stateAdd;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private RawImage colorImage;
    [SerializeField] private Image readyImage;
    [SerializeField] private Image archetypeImage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private GameObject sliderContainer;
    [SerializeField] private Image backgroundRaycast;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button botArchetypePrevButton;
    [SerializeField] private Button botArchetypeNextButton;
    [SerializeField] private Button addBotButton;
    public Transform root;

    [Header("Visuals")]
    [SerializeField] private Sprite spriteReady;

    [SerializeField] private Sprite spriteNotReady;
    [SerializeField] private Sprite[] spriteArchetypes;
    [SerializeField] private Shader colorShader;
    private ulong _steamId;
    private ulong _botId;
    private bool _isMe;
    private Action<ulong> _removeBot;
    private Action<ulong, int> _setBotArchetype;
    private Mode _mode = Mode.AddBot;

    private enum Mode {
        Player,
        Bot,
        AddBot
    }

    void Awake() {
        if (backgroundRaycast != null)
            backgroundRaycast.raycastTarget = true;

        colorImage.material = new Material(colorShader);
        SetHover(false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!_isMe)
            SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        SetHover(false);
    }

    private void UpdateState() {
        stateAdd.SetActive(_mode == Mode.AddBot);
        stateItem.SetActive(_mode != Mode.AddBot);
    }

    void SetHover(bool isHover) {
        UpdateState();

        nameText.gameObject.SetActive(!isHover);
        sliderContainer.SetActive(isHover && _mode == Mode.Player);
        if (_mode == Mode.AddBot) return;

        var isHost = NetworkManager.Singleton.IsServer;
        botArchetypeNextButton.gameObject.SetActive(isHost && isHover && _mode == Mode.Bot);
        botArchetypePrevButton.gameObject.SetActive(isHost && isHover && _mode == Mode.Bot);
        kickButton.gameObject.SetActive(isHost && isHover);
    }

    private void OnEnable() {
        volumeSlider.onValueChanged.AddListener(VolumeChanged);
        kickButton.onClick.AddListener(() => LobbyManager.Instance.KickPlayer(_steamId));
    }

    private void OnDisable() {
        volumeSlider.onValueChanged.RemoveAllListeners();
        kickButton.onClick.RemoveAllListeners();
        botArchetypeNextButton.onClick.RemoveAllListeners();
        botArchetypePrevButton.onClick.RemoveAllListeners();
    }

    private void VolumeChanged(float value) {
        PlayersVoiceSettings.SetVolume(_steamId, value);
    }

    public void UpdateState(Friend member) {
        _mode = Mode.Player;
        _isMe = member.IsMe;
        var image = member.IsReady() ? spriteReady : spriteNotReady;
        readyImage.gameObject.SetActive(true);
        readyImage.overrideSprite = image;
        var color = member.GetColor();
        colorImage.material.SetFloat(ColorizeMesh.Hue, color.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
        var d = PlayerManager.Instance.FindBySteamId(member.Id.Value);
        if (d.HasValue && d.Value.Archetype >= 0)
            archetypeImage.overrideSprite = spriteArchetypes[d.Value.Archetype];
        else
            archetypeImage.overrideSprite = null;

        kickButton.onClick.RemoveAllListeners();
        kickButton.onClick.AddListener(() => LobbyManager.Instance.KickPlayer(_steamId));
        UpdateState();
    }

    public void UpdateAddBotState(Action action) {
        _mode = Mode.AddBot;
        _isMe = false;
        addBotButton.onClick.RemoveAllListeners();
        addBotButton.onClick.AddListener(() => action?.Invoke());
        UpdateState();
    }

    public void UpdateBotState(
        LobbyBotRosterData.Entry bot,
        Action<ulong> onRemove,
        Action<ulong, int> onSetArchetype
    ) {
        _mode = Mode.Bot;
        _isMe = false;
        _botId = bot.id;

        nameText.text = $"Bot #{bot.id}";
        readyImage.gameObject.SetActive(false);
        if (bot.archetype >= 0 && bot.archetype < spriteArchetypes.Length)
            archetypeImage.overrideSprite = spriteArchetypes[bot.archetype];
        else
            archetypeImage.overrideSprite = null;

        colorImage.material.SetFloat(ColorizeMesh.Hue, bot.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, bot.saturation);

        volumeSlider.gameObject.SetActive(false);

        kickButton.onClick.RemoveAllListeners();
        kickButton.onClick.AddListener(() => onRemove(_botId));
        var prev = (bot.archetype - 1 + spriteArchetypes.Length) % spriteArchetypes.Length;
        botArchetypePrevButton.onClick.RemoveAllListeners();
        botArchetypePrevButton.onClick.AddListener(() => onSetArchetype(_botId, prev));
        var next = (bot.archetype + 1) % spriteArchetypes.Length;
        botArchetypeNextButton.onClick.RemoveAllListeners();
        botArchetypeNextButton.onClick.AddListener(() => onSetArchetype(_botId, next));
        UpdateState();
    }

    public void UpdateName(string playerName, ulong steamId) {
        _steamId = steamId;
        volumeSlider.value = PlayersVoiceSettings.Volume(steamId);
        nameText.text = playerName;

        var color = new Friend(steamId).GetColor();
        colorImage.material.SetFloat(ColorizeMesh.Hue, color.hue);
        colorImage.material.SetFloat(ColorizeMesh.Saturation, color.saturation);
    }
}