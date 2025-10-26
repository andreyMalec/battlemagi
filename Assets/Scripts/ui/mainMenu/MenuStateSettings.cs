using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuStateSettings : MonoBehaviour {
    [SerializeField] private Button buttonSettingsSound;
    [SerializeField] private Button buttonSettingsSoundBack;
    [SerializeField] private Button buttonSettingsGraphic;
    [SerializeField] private Button buttonSettingsGraphicBack;

    [Header("State")]
    [SerializeField] private GameObject stateSettingsSound;

    [SerializeField] private GameObject stateSettingsGraphic;

    private State _state;
    private Menu _menu;

    private enum State {
        SettingsSound,
        SettingsGraphic,
    }

    private void Awake() {
        _menu = GetComponentInParent<Menu>();
    }

    private void OnEnable() {
        buttonSettingsSoundBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphic.onClick.AddListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.AddListener(OnSettingsSoundClick);
    }

    private void Update() {
        stateSettingsSound.gameObject.SetActive(_state == State.SettingsSound);
        stateSettingsGraphic.gameObject.SetActive(_state == State.SettingsGraphic);
    }

    private void OnSettingsSoundClick() {
        _state = State.SettingsSound;
    }

    private void OnSettingsGraphicClick() {
        _state = State.SettingsGraphic;
    }

    private void OnBackClick() {
        _menu.BackToMain();
    }

    private void OnDisable() {
        buttonSettingsSoundBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGraphic.onClick.RemoveListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.RemoveListener(OnSettingsSoundClick);
    }
}