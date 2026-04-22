using System;
using UnityEngine;

public class HandAnimationController : MonoBehaviour {
    private static readonly int Close = Animator.StringToHash("Close");

    private Animator _animator;

    private void Awake() {
        _animator = GetComponent<Animator>();
    }

    private void OnReturnToCaster() {
        SpellLog.Log("_______________ Spell returning to caster: " + transform.parent.name);
        _animator.SetBool(Close, true);
    }
}