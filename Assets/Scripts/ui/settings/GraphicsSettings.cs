using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsSettings : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private TMP_Dropdown vsyncDropdown;
    [SerializeField] private TMP_Dropdown fpsLimitDropdown;
    [SerializeField] private Button applyButton;
    [SerializeField] private CanvasGroup fpsLimitGroup;

    private Resolution[] resolutions;
    private int currentResolutionIndex;

    private void Awake() {
        // Собираем список поддерживаемых разрешений
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++) {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height) {
                currentResolutionIndex = i;
            }
        }

        options = options.Distinct().ToList();

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);

        // Режим окна
        windowModeDropdown.ClearOptions();
        windowModeDropdown.AddOptions(new List<string> { "Fullscreen", "Borderless", "Windowed" });
        windowModeDropdown.value = PlayerPrefs.GetInt("WindowMode", 0);

        // VSync
        vsyncDropdown.ClearOptions();
        vsyncDropdown.AddOptions(new List<string> { "Off", "On" });
        vsyncDropdown.value = PlayerPrefs.GetInt("VSync", 0);
        vsyncDropdown.onValueChanged.AddListener(OnVSyncChanged);

        // FPS Limit
        fpsLimitDropdown.ClearOptions();
        fpsLimitDropdown.AddOptions(new List<string> { "30", "60", "120", "144", "240", "Unlimited" });
        fpsLimitDropdown.value = PlayerPrefs.GetInt("FPSLimit", 1);

        applyButton.onClick.AddListener(ApplySettings);
    }

    private void OnVSyncChanged(int ind) {
        fpsLimitGroup.alpha = ind == 0 ? 1f : 0.25f;
        fpsLimitGroup.interactable = ind == 0;
    }

    private void ApplySettings() {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("WindowMode", windowModeDropdown.value);
        PlayerPrefs.SetInt("VSync", vsyncDropdown.value);
        PlayerPrefs.SetInt("FPSLimit", fpsLimitDropdown.value);
        PlayerPrefs.Save();

        GraphicsSettingsInitializer.ApplySavedSettings();
    }
}