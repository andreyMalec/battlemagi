using UnityEngine;

public class DisablePlayer : MonoBehaviour {
    public bool Disabled { get; private set; }

    private StateController _stateController;
    private PlayerTester _tester;
    private SpellCasterPlayer _caster;
    private FirstPersonLook _look;
    private PlayerAnimator _playerAnimator;
    private BotAnimator _botAnimator;
    private FootControllerIK _footControllerIK;
    private BotMovement _botMovement;
    private BotMovementController _botMovementController;
    private CameraZoom _cameraZoom;

    private void Awake() {
        _stateController = GetComponent<StateController>();
        _tester = GetComponent<PlayerTester>();
        _caster = GetComponent<SpellCasterPlayer>();
        _look = GetComponent<FirstPersonLook>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _botAnimator = GetComponent<BotAnimator>();
        _botMovement = GetComponent<BotMovement>();
        _botMovementController = GetComponent<BotMovementController>();
        _cameraZoom = GetComponent<CameraZoom>();
    }

    public void BindAvatar(FootControllerIK footControllerIK) {
        _footControllerIK = footControllerIK;
    }

    public void SetDisablePlayer(bool active) {
        Disabled = active;
        if (active) {
            SetDisabled();
        } else {
            SetEnabled();
        }
    }

    private void SetDisabled() {
        if (_stateController != null) _stateController.RefreshMovementState();
        if (_caster != null) _caster.enabled = false;
        if (_look != null) _look.enabled = false;
        if (_cameraZoom != null) _cameraZoom.enabled = false;
        if (_playerAnimator != null) {
            _playerAnimator.enabled = false;
            _playerAnimator.AnimatorSpeed(0f);
        }

        if (_botAnimator != null) {
            _botAnimator.enabled = false;
            _botAnimator.AnimatorSpeed(0f);
        }

        if (_footControllerIK != null) _footControllerIK.enabled = false;
        if (_tester != null) _tester.enabled = false;
        if (_botMovement != null) _botMovement.enabled = false;
        if (_botMovementController != null) _botMovementController.enabled = false;
    }

    private void SetEnabled() {
        if (_stateController != null) _stateController.RefreshMovementState();
        if (_caster != null) _caster.enabled = true;
        if (_look != null) _look.enabled = true;
        if (_cameraZoom != null) _cameraZoom.enabled = true;
        if (_playerAnimator != null) {
            _playerAnimator.enabled = true;
            _playerAnimator.AnimatorSpeed(1f);
        }

        if (_botAnimator != null) {
            _botAnimator.enabled = true;
            _botAnimator.AnimatorSpeed(1f);
        }

        if (_footControllerIK != null) _footControllerIK.enabled = true;
        if (_tester != null) _tester.enabled = true;
        if (_botMovement != null) _botMovement.enabled = true;
        if (_botMovementController != null) _botMovementController.enabled = true;
    }
}