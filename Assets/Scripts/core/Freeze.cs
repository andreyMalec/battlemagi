using System;
using UnityEngine;

public class Freeze : MonoBehaviour {
    private FirstPersonMovement _movement;
    private PlayerSpellCaster _caster;
    private FirstPersonLook _look;
    private Animator _animator;
    private PlayerAnimator _playerAnimator;
    private FootControllerIK _footControllerIK;

    private void Awake() {
        var parent = transform.parent.gameObject;
        _movement = parent.GetComponent<FirstPersonMovement>();
        _caster = parent.GetComponent<PlayerSpellCaster>();
        _look = parent.GetComponent<FirstPersonLook>();
        _playerAnimator = parent.GetComponent<PlayerAnimator>();
    }

    public void BindAvatar(Animator a, FootControllerIK footControllerIK) {
        _animator = a;
        _footControllerIK = footControllerIK;
    }

    private void OnEnable() {
        Debug.Log($"Игрок {_movement.gameObject.name} замерз");
        _movement.enabled = false;
        _caster.enabled = false;
        _look.enabled = false;
        _playerAnimator.enabled = false;
        _animator.speed = 0f;
        _footControllerIK.enabled = false;
    }

    private void OnDisable() {
        Debug.Log($"Игрок {_movement.gameObject.name} оттаял");
        _movement.enabled = true;
        _caster.enabled = true;
        _look.enabled = true;
        _playerAnimator.enabled = true;
        _animator.speed = 1f;
        _footControllerIK.enabled = true;
    }
}