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
        if (mainMixer == null) return;
        const float eps = 0.0001f; // avoid log(0)

        float clamped = Mathf.Max(value, eps);
        float dB;

        if (clamped <= 1f) {
            // 0..1: normal attenuation (negative dB)
            dB = Mathf.Log10(clamped) * 20f;
        } else {
            float mirrorX = Mathf.Min(clamped, 2f);
            float t = Mathf.Max(2f - mirrorX, eps);
            dB = -Mathf.Log10(t) * 20f;
        }

        mainMixer.SetFloat(param, dB);
    }
}