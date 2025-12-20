using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuStateSettings : MonoBehaviour {
    [SerializeField] private Button buttonSettingsSound;
    [SerializeField] private Button buttonSettingsSoundBack;
    [SerializeField] private Button buttonSettingsGraphic;
    [SerializeField] private Button buttonSettingsGraphicBack;
    [SerializeField] private Button buttonGeneral;
    [SerializeField] private Button buttonGeneralBack;

    [Header("State")]
    [SerializeField] private GameObject stateSettingsSound;

    [SerializeField] private GameObject stateSettingsGraphic;
    [SerializeField] private GameObject stateGeneral;

    private State _state;
    private Menu _menu;

    private enum State {
        SettingsSound,
        SettingsGraphic,
        SettingsGeneral,
    }

    private void Awake() {
        _menu = GetComponentInParent<Menu>();
    }

    private void OnEnable() {
        buttonSettingsSoundBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.AddListener(OnBackClick);
        buttonGeneralBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphic.onClick.AddListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.AddListener(OnSettingsSoundClick);
        buttonGeneral.onClick.AddListener(OnSettingsGeneralClick);
    }

    private void Update() {
        stateSettingsSound.gameObject.SetActive(_state == State.SettingsSound);
        stateSettingsGraphic.gameObject.SetActive(_state == State.SettingsGraphic);
        stateGeneral.gameObject.SetActive(_state == State.SettingsGeneral);
    }

    private void OnSettingsSoundClick() {
        _state = State.SettingsSound;
    }

    private void OnSettingsGeneralClick() {
        _state = State.SettingsGeneral;
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
        buttonGeneralBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGraphic.onClick.RemoveListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.RemoveListener(OnSettingsSoundClick);
        buttonGeneral.onClick.RemoveListener(OnSettingsGeneralClick);
    }
}