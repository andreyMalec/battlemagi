using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerVoiceItem : MonoBehaviour {
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Slider volumeSlider;
    private ulong _steamId;

    private void OnEnable() {
        volumeSlider.onValueChanged.AddListener(VolumeChanged);
    }

    private void OnDisable() {
        volumeSlider.onValueChanged.RemoveAllListeners();
    }

    public void UpdateItem(ulong steamId, string playerName, float volume) {
        nameText.text = playerName;
        volumeSlider.value = volume;
        _steamId = steamId;
    }

    private void VolumeChanged(float value) {
        PlayersVoiceSettings.SetVolume(_steamId, value);
    }
}