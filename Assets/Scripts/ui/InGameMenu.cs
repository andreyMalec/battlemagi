using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenu : MonoBehaviour {
    [Header("State")]
    [SerializeField] private GameObject stateMain;

    [SerializeField] private GameObject stateSettings;
    [SerializeField] private GameObject stateSettingsSound;
    [SerializeField] private GameObject stateSettingsGraphic;
    [SerializeField] private GameObject stateSettingsGeneral;

    [Header("Main")]
    [SerializeField] private GameObject[] hideInMenu;

    [SerializeField] private GameObject container;
    [SerializeField] private Button buttonBack;
    [SerializeField] private Button buttonSettings;
    [SerializeField] private Button buttonExit;

    [Header("Settings")]
    [SerializeField] private Button buttonSettingsSound;

    [SerializeField] private Button buttonSettingsSoundBack;
    [SerializeField] private Button buttonSettingsGraphic;
    [SerializeField] private Button buttonSettingsGraphicBack;
    [SerializeField] private Button buttonSettingsGeneral;
    [SerializeField] private Button buttonSettingsGeneralBack;

    private State _state;

    private enum State {
        Main,
        SettingsSound,
        SettingsGraphic,
        SettingsGeneral,
    }

    private void OnEnable() {
        buttonBack.onClick.AddListener(OnBackClick);
        buttonSettingsSoundBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.AddListener(OnBackClick);
        buttonSettingsGeneralBack.onClick.AddListener(OnBackClick);
        buttonSettings.onClick.AddListener(OnSettingsClick);
        buttonExit.onClick.AddListener(OnExitClick);
        buttonSettingsGraphic.onClick.AddListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.AddListener(OnSettingsSoundClick);
        buttonSettingsGeneral.onClick.AddListener(OnSettingsGeneralClick);
    }

    private void OnBackClick() {
        ToggleMenu();
    }

    private void OnSettingsClick() {
        _state = State.SettingsGraphic;
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

    private void OnExitClick() {
        var lobby = LobbyManager.Instance.CurrentLobby;
        if (lobby.HasValue) {
            LobbyManager.Instance.LeaveLobby();
            TeamManager.Instance.Reset();
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleMenu();
        }

        if (!container.gameObject.activeSelf)
            ShowCursor(Input.GetKey(KeyCode.LeftAlt));

        stateMain.gameObject.SetActive(_state == State.Main);
        stateSettings.gameObject.SetActive(_state != State.Main);
        stateSettingsSound.gameObject.SetActive(_state == State.SettingsSound);
        stateSettingsGraphic.gameObject.SetActive(_state == State.SettingsGraphic);
        stateSettingsGeneral.gameObject.SetActive(_state == State.SettingsGeneral);
    }

    private void ToggleMenu() {
        if (_state == State.SettingsSound || _state == State.SettingsGraphic || _state == State.SettingsGeneral) {
            _state = State.Main;
            return;
        }

        var active = !container.gameObject.activeSelf;
        container.gameObject.SetActive(active);
        foreach (var obj in hideInMenu) {
            obj.gameObject.SetActive(!active);
        }

        ShowCursor(active);
    }

    private void ShowCursor(bool isVisible) {
        if (isVisible) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable() {
        buttonBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsSoundBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.RemoveListener(OnBackClick);
        buttonSettingsGeneralBack.onClick.RemoveListener(OnBackClick);
        buttonSettings.onClick.RemoveListener(OnSettingsClick);
        buttonExit.onClick.RemoveListener(OnExitClick);
        buttonSettingsGraphic.onClick.RemoveListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.RemoveListener(OnSettingsSoundClick);
        buttonSettingsGeneral.onClick.RemoveListener(OnSettingsSoundClick);
    }
}