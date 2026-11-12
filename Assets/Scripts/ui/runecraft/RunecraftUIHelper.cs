using System;
using UnityEngine;
using UnityEngine.UI;

public class RunecraftUIHelper : MonoBehaviour {
    [SerializeField] private GameObject helperUI;
    [SerializeField] private Button toggleHelper;
    private bool _alreadySeen;
    private bool _visible;

    private void Awake() {
        _alreadySeen = PlayerPrefs.GetInt("RunecraftUIHelperSeen", 0) != 0;
        if (!_alreadySeen) {
            Toggle();
        }

        helperUI.gameObject.SetActive(_visible);
        toggleHelper.onClick.AddListener(Toggle);
    }

    public void Toggle() {
        _visible = !_visible;
        helperUI.gameObject.SetActive(_visible);
        if (!_visible && !_alreadySeen) {
            PlayerPrefs.SetInt("RunecraftUIHelperSeen", 1);
        }
    }
}