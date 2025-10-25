using UnityEngine;

public class GraphicsSettingsInitializer : MonoBehaviour
{
    private void Awake()
    {
        ApplySavedSettings();
    }

    public static void ApplySavedSettings()
    {
        // ====== Resolution ======
        Resolution[] resolutions = Screen.resolutions;
        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutions.Length - 1);
        resIndex = Mathf.Clamp(resIndex, 0, resolutions.Length - 1);
        Resolution res = resolutions[resIndex];

        // ====== Window Mode ======
        int windowModeIndex = PlayerPrefs.GetInt("WindowMode", 1);
        FullScreenMode mode = FullScreenMode.FullScreenWindow;
        switch (windowModeIndex)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.FullScreenWindow; break;
            case 2: mode = FullScreenMode.Windowed; break;
        }

        Screen.SetResolution(res.width, res.height, mode);

        // ====== VSync ======
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync", 0);

        // ====== FPS Limit ======
        int fpsIndex = PlayerPrefs.GetInt("FPSLimit", 1);
        switch (fpsIndex)
        {
            case 0: Application.targetFrameRate = 30; break;
            case 1: Application.targetFrameRate = 60; break;
            case 2: Application.targetFrameRate = 120; break;
            case 3: Application.targetFrameRate = 144; break;
            case 4: Application.targetFrameRate = 240; break;
            case 5: Application.targetFrameRate = -1; break;
        }

        Debug.Log(
            $"[GraphicsSettings] Applied: {res.width}x{res.height}, {mode}, VSync={QualitySettings.vSyncCount}, FPS={Application.targetFrameRate}");
    }
}