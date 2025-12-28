using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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
    private List<Vector2Int> uniqueResolutions; // width/height pairs
    private int currentResolutionIndex;

    private void Awake() {
        // Собираем список поддерживаемых разрешений
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        // Построим список уникальных по размеру разрешений (игнорируем частоту обновления)
        uniqueResolutions = new List<Vector2Int>();
        for (int i = 0; i < resolutions.Length; i++) {
            var wh = new Vector2Int(resolutions[i].width, resolutions[i].height);
            if (!uniqueResolutions.Contains(wh)) uniqueResolutions.Add(wh);
        }

        // Отсортируем по размеру (сначала меньшие)
        uniqueResolutions = uniqueResolutions
            .OrderBy(v => v.x)
            .ThenBy(v => v.y)
            .ToList();

        List<string> options = new List<string>();
        currentResolutionIndex = 0;

        // Текущий размер окна/экрана
        int curW = Screen.width;
        int curH = Screen.height;

        for (int i = 0; i < uniqueResolutions.Count; i++) {
            var wh = uniqueResolutions[i];
            string option = $"{wh.x} x {wh.y}";
            options.Add(option);

            // Выберем текущий индекс по фактическому размеру окна
            if (wh.x == curW && wh.y == curH) {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // Восстановим сохранённое значение (width/height), при отсутствии — используем текущий
        int savedW = PlayerPrefs.GetInt("ResolutionWidth", uniqueResolutions[currentResolutionIndex].x);
        int savedH = PlayerPrefs.GetInt("ResolutionHeight", uniqueResolutions[currentResolutionIndex].y);
        int savedIndex = currentResolutionIndex;
        for (int i = 0; i < uniqueResolutions.Count; i++) {
            if (uniqueResolutions[i].x == savedW && uniqueResolutions[i].y == savedH) {
                savedIndex = i;
                break;
            }
        }

        resolutionDropdown.value = savedIndex;

        vsyncDropdown.onValueChanged.AddListener(OnVSyncChanged);

        applyButton.onClick.AddListener(ApplySettings);
    }

    private void OnEnable() {
        // Режим окна
        windowModeDropdown.ClearOptions();
        var fullscreen = R.String("settings.fullscreen");
        var borderless = R.String("settings.borderless");
        var windowed = R.String("settings.windowed");
        windowModeDropdown.AddOptions(new List<string> { fullscreen, borderless, windowed });
        windowModeDropdown.value = PlayerPrefs.GetInt("WindowMode", 0);

        // VSync
        vsyncDropdown.ClearOptions();
        var off = R.String("settings.off");
        var on = R.String("settings.on");
        vsyncDropdown.AddOptions(new List<string> { off, on });
        vsyncDropdown.value = PlayerPrefs.GetInt("VSync", 0);
        OnVSyncChanged(vsyncDropdown.value);

        // FPS Limit
        fpsLimitDropdown.ClearOptions();
        var unlimited = R.String("settings.unlimited");
        fpsLimitDropdown.AddOptions(new List<string> { "30", "60", "120", "144", "240", unlimited });
        fpsLimitDropdown.value = PlayerPrefs.GetInt("FPSLimit", 1);
    }

    private void OnVSyncChanged(int ind) {
        fpsLimitGroup.alpha = ind == 0 ? 1f : 0.25f;
        fpsLimitGroup.interactable = ind == 0;
    }

    private void ApplySettings() {
        // Сохраняем выбор пользователя
        int selIndex = resolutionDropdown.value;
        Vector2Int selWH = uniqueResolutions[Mathf.Clamp(selIndex, 0, uniqueResolutions.Count - 1)];
        int windowModeIndex = windowModeDropdown.value;
        int vsync = vsyncDropdown.value;
        int fpsIndex = fpsLimitDropdown.value;

        // 5) Сохраняем новые значения
        PlayerPrefs.SetInt("ResolutionWidth", selWH.x);
        PlayerPrefs.SetInt("ResolutionHeight", selWH.y);
        PlayerPrefs.SetInt("WindowMode", windowModeIndex);
        PlayerPrefs.SetInt("VSync", vsync);
        PlayerPrefs.SetInt("FPSLimit", fpsIndex);
        PlayerPrefs.Save();

        SettingsInitializer.ApplySavedSettings();
    }
}