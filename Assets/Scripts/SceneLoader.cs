using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public bool waitUntilModelLoaded;

    private void Start() {
        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu() {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null && (WhisperHolder.checkVM() || !waitUntilModelLoaded ||
                                                 WhisperHolder.instance.whisper.IsLoaded));
        SceneManager.LoadScene("MainMenu");
    }
}