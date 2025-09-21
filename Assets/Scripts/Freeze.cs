using System;
using UnityEngine;

public class Freeze : MonoBehaviour {
    private FirstPersonMovement _movement;
    private PlayerSpellCaster _caster;
    private FirstPersonLook _look;
    private Animator _animator;
    private PlayerAnimator _playerAnimator;

    private void Awake() {
        var parent = transform.parent.gameObject;
        _movement = parent.GetComponent<FirstPersonMovement>();
        _caster = parent.GetComponent<PlayerSpellCaster>();
        _look = parent.GetComponent<FirstPersonLook>();
        _animator = parent.GetComponentInChildren<Animator>();
        _playerAnimator = parent.GetComponent<PlayerAnimator>();
    }

    private void OnEnable() {
        Debug.Log("Игрок замерз");
        _movement.enabled = false;
        _caster.enabled = false;
        _look.enabled = false;
        _playerAnimator.enabled = false;
        _animator.speed = 0f;
    }

    private void OnDisable() {
        Debug.Log("Игрок оттаял");
        _movement.enabled = true;
        _caster.enabled = true;
        _look.enabled = true;
        _playerAnimator.enabled = true;
        _animator.speed = 1f;
    }
}