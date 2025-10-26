using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour {
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider voiceSlider;
    [SerializeField] private Slider effectSlider;

    private void Start() {
        masterSlider.value = PlayerPrefs.GetFloat("VolumeMaster", 1f);
        voiceSlider.value = PlayerPrefs.GetFloat("VolumeVoice", 1f);
        effectSlider.value = PlayerPrefs.GetFloat("VolumeEffect", 0.25f);

        ApplyVolume();
    }

    public void ApplyVolume() {
        SetVolume("MasterVolume", masterSlider.value);
        SetVolume("VoiceVolume", voiceSlider.value);
        SetVolume("EffectVolume", effectSlider.value);
    }

    private void SetVolume(string param, float value) {
        // громкость в микшере задаётся в децибелах (dB)
        float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        mainMixer.SetFloat(param, dB);
        PlayerPrefs.SetFloat(param, value);
    }
}