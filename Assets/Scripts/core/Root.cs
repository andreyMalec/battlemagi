using UnityEngine;

public class Root : MonoBehaviour {
    private StateController _stateController;
    private PlayerTester _tester;
    private SpellCasterPlayer _caster;
    private BotMovement _botMovement;
    private BotMovementController _botMovementController;

    private void Awake() {
        var parent = transform.parent.gameObject;
        _stateController = parent.GetComponent<StateController>();
        _tester = parent.GetComponent<PlayerTester>();
        _caster = parent.GetComponent<SpellCasterPlayer>();
        _botMovement = parent.GetComponent<BotMovement>();
        _botMovementController = parent.GetComponent<BotMovementController>();
    }

    private void OnEnable() {
        Debug.Log($"Игрок {_caster.gameObject.name} не может двигаться");
        if (_stateController != null) _stateController.RefreshMovementState();

        if (_tester != null) _tester.enabled = false;
        if (_botMovement != null) _botMovement.enabled = false;
        if (_botMovementController != null) _botMovementController.enabled = false;
    }

    private void OnDisable() {
        Debug.Log($"Игрок {_caster.gameObject.name} может двигаться");
        if (_stateController != null) _stateController.RefreshMovementState();

        if (_tester != null) _tester.enabled = true;
        if (_botMovement != null) _botMovement.enabled = true;
        if (_botMovementController != null) _botMovementController.enabled = true;
    }
}