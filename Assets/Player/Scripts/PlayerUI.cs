using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour {
    private PlayerUIRenderer _renderer;
    private Damageable _damageable;
    private PlayerSpellCaster _caster;
    private FirstPersonMovement _movement;

    private void Awake() {
        _renderer = FindFirstObjectByType<PlayerUIRenderer>();
        _damageable = GetComponent<Damageable>();
        _caster = GetComponent<PlayerSpellCaster>();
        _movement = GetComponent<FirstPersonMovement>();
    }

    private void Update() {
        if (_renderer == null) return;
        if (!_damageable.IsOwner) return;

        var hp = Math.Clamp(_damageable.health.Value / _damageable.maxHealth, 0, 1);
        _renderer.hp.transform.localScale = new Vector3(hp, 1, 1);
        _renderer.hpText.text = $"{_damageable.health.Value:0}/{_damageable.maxHealth:0}";

        var mp = Math.Clamp(_caster.mana.Value / _caster.maxMana, 0, 1);
        _renderer.mp.transform.localScale = new Vector3(mp, 1, 1);
        _renderer.mpText.text = $"{_caster.mana.Value:0}/{_caster.maxMana:0}";

        var stamina = Math.Clamp(_movement.stamina.Value / _movement.maxStamina, 0, 1);
        _renderer.stamina.transform.localScale = new Vector3(stamina, 1, 1);
    }
}