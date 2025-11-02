using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour {
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider voiceSlider;
    [SerializeField] private Slider effectSlider;
    [SerializeField] private Slider musicSlider;

    [Header("Range")]
    [SerializeField] private float minValue = 0f;

    [SerializeField] private float maxValue = 1.5f;

    private void Start() {
        SetupSlider(masterSlider, "MasterVolume", 1f);
        SetupSlider(voiceSlider, "VoiceVolume", 1f);
        SetupSlider(effectSlider, "EffectVolume", 0.25f);
        SetupSlider(musicSlider, "MusicVolume", 0.25f);
    }

    private void OnEnable() {
        masterSlider.onValueChanged.AddListener(v => OnSliderChanged("MasterVolume", v));
        voiceSlider.onValueChanged.AddListener(v => OnSliderChanged("VoiceVolume", v));
        effectSlider.onValueChanged.AddListener(v => OnSliderChanged("EffectVolume", v));
        musicSlider.onValueChanged.AddListener(v => OnSliderChanged("MusicVolume", v));
    }

    private void OnDisable() {
        masterSlider.onValueChanged.RemoveAllListeners();
        voiceSlider.onValueChanged.RemoveAllListeners();
        effectSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
    }

    private void SetupSlider(Slider slider, string key, float defaultValue) {
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        float saved = PlayerPrefs.GetFloat(key, defaultValue);
        saved = Mathf.Clamp(saved, minValue, maxValue);

        slider.value = saved;
        ApplyVolume(key, saved);
    }

    private void OnSliderChanged(string key, float value) {
        value = Mathf.Clamp(value, minValue, maxValue);
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        ApplyVolume(key, value);
    }

    private void ApplyVolume(string param, float value) {
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