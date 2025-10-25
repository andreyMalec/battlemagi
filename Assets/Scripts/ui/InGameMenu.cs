using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenu : MonoBehaviour {
    [Header("State")]
    [SerializeField] private GameObject stateMain;

    [SerializeField] private GameObject stateSettings;
    [SerializeField] private GameObject stateSettingsSound;
    [SerializeField] private GameObject stateSettingsGraphic;

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

    private State _state;

    private enum State {
        Main,
        SettingsSound,
        SettingsGraphic,
    }

    private void OnEnable() {
        buttonBack.onClick.AddListener(OnBackClick);
        buttonSettingsSoundBack.onClick.AddListener(OnBackClick);
        buttonSettingsGraphicBack.onClick.AddListener(OnBackClick);
        buttonSettings.onClick.AddListener(OnSettingsClick);
        buttonExit.onClick.AddListener(OnExitClick);
        buttonSettingsGraphic.onClick.AddListener(OnSettingsGraphicClick);
        buttonSettingsSound.onClick.AddListener(OnSettingsSoundClick);
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
    }

    private void ToggleMenu() {
        if (_state == State.SettingsSound || _state == State.SettingsGraphic) {
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
}