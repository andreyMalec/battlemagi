using UnityEngine;

public class Freeze : MonoBehaviour {
    private StateController _stateController;
    private PlayerTester _tester;
    private SpellCasterPlayer _caster;
    private FirstPersonLook _look;
    private Animator _animator;
    private PlayerAnimator _playerAnimator;
    private FootControllerIK _footControllerIK;
    private BotMovement _botMovement;
    private BotMovementController _botMovementController;

    private void Awake() {
        var parent = transform.parent.gameObject;
        _stateController = parent.GetComponent<StateController>();
        _tester = parent.GetComponent<PlayerTester>();
        _caster = parent.GetComponent<SpellCasterPlayer>();
        _look = parent.GetComponent<FirstPersonLook>();
        _playerAnimator = parent.GetComponent<PlayerAnimator>();
        _botMovement = parent.GetComponent<BotMovement>();
        _botMovementController = parent.GetComponent<BotMovementController>();
    }

    public void BindAvatar(Animator a, FootControllerIK footControllerIK) {
        _animator = a;
        _footControllerIK = footControllerIK;
    }

    private void OnEnable() {
        Debug.Log($"Игрок {_caster.gameObject.name} замерз");
        if (_stateController != null) _stateController.RefreshMovementState();
        if (_caster != null) _caster.enabled = false;
        if (_look != null) _look.enabled = false;
        if (_playerAnimator != null) _playerAnimator.enabled = false;
        if (_animator != null) _animator.speed = 0f;
        if (_footControllerIK != null) _footControllerIK.enabled = false;
        if (_tester != null) _tester.enabled = false;
        if (_botMovement != null) _botMovement.enabled = false;
        if (_botMovementController != null) _botMovementController.enabled = false;
    }

    private void OnDisable() {
        Debug.Log($"Игрок {_caster.gameObject.name} оттаял");
        if (_stateController != null) _stateController.RefreshMovementState();
        if (_caster != null) _caster.enabled = true;
        if (_look != null) _look.enabled = true;
        if (_playerAnimator != null) _playerAnimator.enabled = true;
        if (_animator != null) _animator.speed = 1f;
        if (_footControllerIK != null) _footControllerIK.enabled = true;
        if (_tester != null) _tester.enabled = true;
        if (_botMovement != null) _botMovement.enabled = true;
        if (_botMovementController != null) _botMovementController.enabled = true;
    }
}