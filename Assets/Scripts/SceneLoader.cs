using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public bool waitUntilModelLoaded;

    private void Start() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu() {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null && (!waitUntilModelLoaded || WhisperHolder.instance.whisper.IsLoaded));
        SceneManager.LoadScene("MainMenu");
    }
}