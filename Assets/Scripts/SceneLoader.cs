using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voice;

public class SceneLoader : MonoBehaviour {
    public bool waitUntilModelLoaded;

    private void Start() {
        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu() {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null && (SpeechToTextHolder.Instance.IsInitialized || SpeechToTextHolder.RunningOnVM()));
        SceneManager.LoadScene("MainMenu");
    }
}