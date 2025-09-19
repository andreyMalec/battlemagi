using System;
using UnityEngine;
using UnityEngine.UI;
//TODO Net obj ?
public class PlayerUI : MonoBehaviour {
    private PlayerUIRenderer _renderer;
    private Damageable _damageable;
    private FirstPersonMovement _movement;

    private void Awake() {
        _renderer = FindFirstObjectByType<PlayerUIRenderer>();
        _damageable = GetComponent<Damageable>();
        _movement = GetComponent<FirstPersonMovement>();
    }

    private void Update() {
        if (_renderer == null) return;

        var hp = Math.Clamp(_damageable.health.Value / _damageable.maxHealth, 0, 1);
        _renderer.hp.transform.localScale = new Vector3(hp, 1, 1);

        var stamina = Math.Clamp(_movement.stamina.Value / _movement.movementSettings.maxStamina, 0, 1);
        _renderer.stamina.transform.localScale = new Vector3(stamina, 1, 1);
    }
}