using UnityEngine;
using UnityEngine.Audio;

public class SoundsSettingsInitializer : MonoBehaviour {
    [SerializeField] private AudioMixer mainMixer;

    private void Awake() {
        ApplyVolume("MasterVolume", 1f);
        ApplyVolume("VoiceVolume", 1f);
        ApplyVolume("EffectVolume", 0.25f);
        ApplyVolume("MusicVolume", 0.25f);
    }

    private void ApplyVolume(string param, float defaultValue) {
        float value = PlayerPrefs.GetFloat(param, defaultValue);
        AudioSettings.ApplyVolume(mainMixer, param, value);
    }
}