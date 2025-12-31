using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Voice;

public class MicrophoneSettings : MonoBehaviour {
    [SerializeField] private TMP_Dropdown microphoneDropdown;
    [SerializeField] private Image microphoneActivity;
    [SerializeField] private Button steamSettings;

    [Header("Config")]
    public string microphoneDefaultLabel = "Default microphone";

    public float sensitivityMultiplier = 5f;
    public float smoothing = 5f;
    public float updateRate = 0.05f;

    private MicrophoneRecord mic;
    private float currentLevel;
    private float targetLevel;

    private bool inGame = false;

    private void Awake() {
        steamSettings.onClick.AddListener(OpenSteamSettings);

        microphoneDropdown.options = Microphone.devices
            .Prepend(microphoneDefaultLabel)
            .Select(text => new TMP_Dropdown.OptionData(text))
            .ToList();

        var selected = PlayerPrefs.GetString("Microphone", microphoneDefaultLabel);
        microphoneDropdown.value = microphoneDropdown.options
            .FindIndex(op => op.text == selected);

        microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
        microphoneActivity.transform.localScale = Vector3.zero;
    }

    private void OnEnable() {
        inGame = LobbyManager.Instance.State == LobbyManager.PlayerState.InGame;
        if (inGame) {
            var player = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (player != null)
                mic = player.GetComponentInChildren<MicrophoneRecord>();
        } else {
            mic = gameObject.AddComponent<MicrophoneRecord>();
            mic.echo = false;
            mic.StartRecord();
        }

        InvokeRepeating(nameof(UpdateMicActivity), 0f, updateRate);
    }

    private void OnDisable() {
        CancelInvoke(nameof(UpdateMicActivity));
        if (!inGame)
            mic.StopRecord();
    }

    private void OnMicrophoneChanged(int ind) {
        if (microphoneDropdown == null || mic == null)
            return;
        currentLevel = targetLevel = 0;

        var opt = microphoneDropdown.options[ind];
        mic.SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        PlayerPrefs.SetString("Microphone", mic.SelectedMicDevice);

        if (inGame)
            mic.GetComponentInParent<Mouth>().ChangeVoice();
    }

    private void UpdateMicActivity() {
        if (microphoneActivity == null || mic == null || !mic.IsRecording)
            return;

        targetLevel = mic.GetAmplitude();
        currentLevel = Mathf.Lerp(currentLevel, targetLevel, Time.deltaTime * smoothing);

        var v = Mathf.Clamp01(currentLevel * sensitivityMultiplier);
        microphoneActivity.color = Color.Lerp(Color.green, Color.red, v);
        microphoneActivity.transform.localScale = new Vector3(v, 1);
    }

    private void OpenSteamSettings() {
        SteamFriends.OpenOverlay("settings");
    }
}