using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SettingsInitializer : MonoBehaviour {
    private void Awake() {
        ApplySavedSettings();
    }

    public static void ApplySavedSettings() {
        // ====== Resolution & Window Mode ======
        int savedW = PlayerPrefs.GetInt("ResolutionWidth", Screen.width);
        int savedH = PlayerPrefs.GetInt("ResolutionHeight", Screen.height);

        int windowModeIndex = PlayerPrefs.GetInt("WindowMode", 0);
        FullScreenMode mode = FullScreenMode.FullScreenWindow;
        switch (windowModeIndex) {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.FullScreenWindow; break;
            case 2: mode = FullScreenMode.Windowed; break;
        }

        bool modeChanged = Screen.fullScreenMode != mode;
        bool sizeChanged = Screen.width != savedW || Screen.height != savedH;
        if (modeChanged || sizeChanged) {
            Screen.SetResolution(savedW, savedH, mode);
        }

        // ====== VSync ======
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync", 0);

        // ====== FPS Limit ======
        int fpsIndex = PlayerPrefs.GetInt("FPSLimit", 1);
        switch (fpsIndex) {
            case 0: Application.targetFrameRate = 30; break;
            case 1: Application.targetFrameRate = 60; break;
            case 2: Application.targetFrameRate = 120; break;
            case 3: Application.targetFrameRate = 144; break;
            case 4: Application.targetFrameRate = 240; break;
            case 5: Application.targetFrameRate = -1; break;
        }

        var languageIndex = PlayerPrefs.GetInt("Language", 0);

        var values = Enum.GetValues(typeof(Language)).Cast<Language>().ToList();
        LocalizationSettings.Instance.SetSelectedLocale(
            Locale.CreateLocale(values[languageIndex].ToString().ToLower()));

        Debug.Log(
            $"[GraphicsSettings] Applied: {savedW}x{savedH}, {mode}, VSync={QualitySettings.vSyncCount}, FPS={Application.targetFrameRate}");
        Debug.Log($"[GeneralSettings] Applied: Language={values[languageIndex].ToString()}");
    }
}