using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InLobbySettings : MonoBehaviour {
    [Header("State")]
    [SerializeField] private GameObject stateSettings;

    [SerializeField] private GameObject stateSettingsSound;
    [SerializeField] private GameObject stateSettingsGraphic;
    [SerializeField] private GameObject stateSettingsGeneral;

    [Header("Settings")]
    [SerializeField] private Button buttonSettings;

    [SerializeField] private Button buttonSettingsSound;
    [SerializeField] private Button buttonSettingsSoundBack;
    [SerializeField] private Button buttonSettingsGraphic;
    [SerializeField] private Button buttonSettingsGraphicBack;
    [SerializeField] private Button buttonSettingsGeneral;
    [SerializeField] private Button buttonSettingsGeneralBack;

    private State _state;

    private enum State {
        Hidden,
        SettingsSound,
        SettingsGraphic,
        SettingsGeneral,
    }

    private void OnEnable() {
        buttonSettings.onClick.AddListener(OnSettingsClick);
        buttonSettingsSoundBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.AddListener(OnBackClick);
        buttonSettingsGeneralBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphic.onClick.AddListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.AddListener(OnSettingsSoundClick);
        buttonSettingsGeneral.onClick.AddListener(OnSettingsGeneralClick);
    }

    private void OnBackClick() {
        ToggleMenu();
    }

    private void OnSettingsClick() {
        _state = State.SettingsSound;
    }

    private void OnSettingsGraphicClick() {
        _state = State.SettingsGraphic;
    }

    private void OnSettingsSoundClick() {
        _state = State.SettingsSound;
    }

    private void OnSettingsGeneralClick() {
        _state = State.SettingsGeneral;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleMenu();
        }

        stateSettings.SetActive(_state != State.Hidden);
        stateSettingsSound.gameObject.SetActive(_state == State.SettingsSound);
        stateSettingsGraphic.gameObject.SetActive(_state == State.SettingsGraphic);
        stateSettingsGeneral.gameObject.SetActive(_state == State.SettingsGeneral);
    }

    private void ToggleMenu() {
        if (_state == State.SettingsSound || _state == State.SettingsGraphic || _state == State.SettingsGeneral) {
            _state = State.Hidden;
            return;
        }

        var active = !stateSettings.activeSelf;
        stateSettings.SetActive(active);
    }

    private void OnDisable() {
        buttonSettings.onClick.RemoveListener(OnSettingsClick);
        buttonSettingsSoundBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGeneralBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGraphic.onClick.RemoveListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.RemoveListener(OnSettingsSoundClick);
        buttonSettingsGeneral.onClick.RemoveListener(OnSettingsSoundClick);
    }
}