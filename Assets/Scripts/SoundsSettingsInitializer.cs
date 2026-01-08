using UnityEngine;
using UnityEngine.Audio;

public class SoundsSettingsInitializer : MonoBehaviour {
    [SerializeField] private AudioMixer mainMixer;

    private void Awake() {
        ApplyVolume("MasterVolume", AudioSettings.MasterVolume);
        ApplyVolume("VoiceVolume", AudioSettings.VoiceVolume);
        ApplyVolume("EffectVolume", AudioSettings.EffectVolume);
        ApplyVolume("MusicVolume", AudioSettings.MusicVolume);
    }

    private void ApplyVolume(string param, float defaultValue) {
        float value = PlayerPrefs.GetFloat(param, defaultValue);
        AudioSettings.ApplyVolume(mainMixer, param, value);
    }
}