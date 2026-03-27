using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisableHud : MonoBehaviour {
    [SerializeField] private KeyCode key = KeyCode.H;

    private bool isHudEnabled = true;

    private void Update() {
        if (Input.GetKeyDown(key)) {
            isHudEnabled = !isHudEnabled;

            foreach (var rootGameObject in SceneManager.GetSceneAt(0).GetRootGameObjects()) {
                foreach (var canvases in rootGameObject.GetComponentsInChildren<Canvas>()) {
                    canvases.enabled = isHudEnabled;
                }

                foreach (var canvases in rootGameObject.GetComponentsInChildren<TMP_Text>()) {
                    canvases.enabled = isHudEnabled;
                }
            }
        }
    }
}