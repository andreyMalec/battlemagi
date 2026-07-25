using System.Collections;
using Unity.Netcode;
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

    private bool _exiting = false;

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
        _exiting = false;
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
        if (_exiting) return;
        _exiting = true;
        StartCoroutine(ExitGame());
    }

    private IEnumerator ExitGame() {
        var lobby = Ctx.CurrentLobby;
        if (lobby.HasValue) {
            if (Ctx.NetManager.IsHost) {
                foreach (var singletonConnectedClient in Ctx.NetManager.ConnectedClients) {
                    if (singletonConnectedClient.Key == Ctx.NetManager.LocalClientId) continue;
                    Ctx.NetManager.DisconnectClient(singletonConnectedClient.Key, "Server closed");
                }
            }

            yield return new WaitForSeconds(0.2f);
            Ctx.Teams.Reset();
            Ctx.LeaveLobby();
            SceneLoader.LoadMenu(true);
        }

        _exiting = false;
    }

    private bool alt = false;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleMenu();
        }

        if (!container.gameObject.activeSelf) {
            if (Input.GetKeyDown(KeyCode.LeftAlt)) {
                alt = !alt;
            }

            ShowCursor(alt);
        }

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