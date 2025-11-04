using UnityEngine;

public class GraphicsSettingsInitializer : MonoBehaviour {
    private void Awake() {
        ApplySavedSettings();
    }

    public static void ApplySavedSettings() {
        // ====== Resolution & Window Mode (from saved width/height) ======
        int savedW;
        int savedH;

        // Backward compatibility: if width/height not set but old index is present
        if (PlayerPrefs.HasKey("ResolutionWidth") && PlayerPrefs.HasKey("ResolutionHeight")) {
            savedW = PlayerPrefs.GetInt("ResolutionWidth");
            savedH = PlayerPrefs.GetInt("ResolutionHeight");
        } else if (PlayerPrefs.HasKey("ResolutionIndex")) {
            Resolution[] all = Screen.resolutions;
            int resIndex = Mathf.Clamp(PlayerPrefs.GetInt("ResolutionIndex", all.Length - 1), 0,
                Mathf.Max(0, all.Length - 1));
            Resolution res = all.Length > 0
                ? all[resIndex]
                : new Resolution { width = Screen.width, height = Screen.height };
            savedW = res.width;
            savedH = res.height;
            PlayerPrefs.SetInt("ResolutionWidth", savedW);
            PlayerPrefs.SetInt("ResolutionHeight", savedH);
            PlayerPrefs.Save();
        } else {
            savedW = Screen.width;
            savedH = Screen.height;
        }

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

        Debug.Log(
            $"[GraphicsSettings] Applied: {savedW}x{savedH}, {mode}, VSync={QualitySettings.vSyncCount}, FPS={Application.targetFrameRate}");
    }
}