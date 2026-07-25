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
            Ctx.NetManager != null &&
            (Ctx.SpeechToText.IsInitialized || SpeechToTextHolder.RunningOnVM()));
        LoadMenu();
    }

    public static void LoadMenu(bool hostDisconnected = false) {
        if (hostDisconnected
            || Ctx.NetManager == null
            || !Ctx.NetManager.IsListening
            || Ctx.NetManager.SceneManager == null)
            SceneManager.LoadScene("MainMenu");
        else
            Ctx.NetManager.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}