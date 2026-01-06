using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour {
    private PlayerUIRenderer _renderer;
    private Damageable _damageable;
    private PlayerSpellCaster _caster;
    private FirstPersonMovement _movement;

    private float _armorBarWidth;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        _renderer = FindFirstObjectByType<PlayerUIRenderer>();
        _damageable = GetComponent<Damageable>();
        _caster = GetComponent<PlayerSpellCaster>();
        _movement = GetComponent<FirstPersonMovement>();
        var ui = GetComponents<UICameraSway>();
        for (var i = 0; i < ui.Length; i++) {
            ui[i].Bind(_renderer.uiContainers[i]);
        }

        _armorBarWidth = _renderer.armor.rect.width;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        _renderer.armor.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _armorBarWidth);
    }

    private void Update() {
        if (_renderer == null) return;
        if (!IsOwner) return;

        var hp = Math.Clamp(_damageable.health.Value / _damageable.maxHealth, 0, 1);
        _renderer.hp.transform.localScale = new Vector3(hp, 1, 1);
        var hpArmor = _damageable.health.Value + _damageable.armor.Value;
        _renderer.hpText.text = $"{hpArmor:0}/{_damageable.maxHealth:0}";

        var armor = Math.Clamp(_damageable.armor.Value / _damageable.maxArmor, 0, 1);
        _renderer.armor.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _armorBarWidth * armor);

        var primalMp = Math.Clamp(_caster.primalMana.Value / _caster.maxMana, 0, 1);
        _renderer.primalMp.transform.localScale = new Vector3(primalMp, 1, 1);
        var mp = Math.Clamp(_caster.mana.Value / _caster.maxMana, 0, 1);
        _renderer.mp.transform.localScale = new Vector3(mp, 1, 1);
        if (_caster.primalMana.Value > 0) {
            _renderer.mpText.text = $"-{_caster.primalMana.Value:0}/{_caster.maxMana:0}";
        } else {
            _renderer.mpText.text = $"{_caster.mana.Value:0}/{_caster.maxMana:0}";
        }

        var stamina = Math.Clamp(_movement.stamina.Value / _movement.maxStamina, 0, 1);
        _renderer.stamina.transform.localScale = new Vector3(stamina, 1, 1);
    }
}