using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    private void Start() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu() {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        SceneManager.LoadScene("MainMenu");
    }
}