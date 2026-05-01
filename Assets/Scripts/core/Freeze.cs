using System;
using UnityEngine;

public class Freeze : MonoBehaviour {
    private FirstPersonMovement _movement;
    private PlayerTester _tester;
    private SpellCasterPlayer _caster;
    private FirstPersonLook _look;
    private Animator _animator;
    private PlayerAnimator _playerAnimator;
    private FootControllerIK _footControllerIK;

    private void Awake() {
        var parent = transform.parent.gameObject;
        _movement = parent.GetComponent<FirstPersonMovement>();
        _tester = parent.GetComponent<PlayerTester>();
        _caster = parent.GetComponent<SpellCasterPlayer>();
        _look = parent.GetComponent<FirstPersonLook>();
        _playerAnimator = parent.GetComponent<PlayerAnimator>();
    }

    public void BindAvatar(Animator a, FootControllerIK footControllerIK) {
        _animator = a;
        _footControllerIK = footControllerIK;
    }

    private void OnEnable() {
        Debug.Log($"Игрок {_caster.gameObject.name} замерз");
        if (_movement != null) _movement.enabled = false;
        if (_caster != null) _caster.enabled = false;
        if (_look != null) _look.enabled = false;
        if (_playerAnimator != null) _playerAnimator.enabled = false;
        if (_animator != null) _animator.speed = 0f;
        if (_footControllerIK != null) _footControllerIK.enabled = false;
        if (_tester != null) _tester.enabled = false;
    }

    private void OnDisable() {
        Debug.Log($"Игрок {_caster.gameObject.name} оттаял");
        if (_movement != null) _movement.enabled = true;
        if (_caster != null) _caster.enabled = true;
        if (_look != null) _look.enabled = true;
        if (_playerAnimator != null) _playerAnimator.enabled = true;
        if (_animator != null) _animator.speed = 1f;
        if (_footControllerIK != null) _footControllerIK.enabled = true;
        if (_tester != null) _tester.enabled = true;
    }
}