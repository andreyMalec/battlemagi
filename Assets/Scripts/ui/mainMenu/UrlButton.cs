using UnityEngine;
using UnityEngine.UI;

public class UrlButton : MonoBehaviour {
    [SerializeField] private string url;
    [SerializeField] private Button button;

    private void Awake() {
        button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        Application.OpenURL(url);
    }
}