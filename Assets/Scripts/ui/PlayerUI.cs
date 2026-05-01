using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour {
    private PlayerUIRenderer _renderer;
    private Damageable _damageable;
    private SpellCasterPlayer _caster;
    private FirstPersonMovement _movement;
    private Statusable _statusable;

    private float _armorBarWidth;

    [SerializeField] private RectTransform echoItemPrefab;
    [SerializeField] private PlayerEffectUIItem effectItemPrefab;
    private readonly List<PlayerEffectUIItem> _items = new();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        _renderer = FindFirstObjectByType<PlayerUIRenderer>();
        _damageable = GetComponent<Damageable>();
        _statusable = GetComponent<Statusable>();
        _caster = GetComponent<SpellCasterPlayer>();
        _movement = GetComponent<FirstPersonMovement>();
        var ui = GetComponentsInChildren<UICameraSway>();
        for (var i = 0; i < ui.Length; i++) {
            ui[i].Bind(_renderer.uiContainers[i]);
        }

        _armorBarWidth = _renderer.armor.rect.width;
        for (int i = _renderer.effectsContainer.childCount - 1; i >= 0; i--) {
            var child = _renderer.effectsContainer.GetChild(i);
            DestroyImmediate(child.gameObject);
        }

        for (int i = _renderer.echoContainer.childCount - 1; i >= 0; i--) {
            var child = _renderer.echoContainer.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (!IsOwner) return;
        if (_renderer != null && _renderer.armor != null)
            _renderer.armor.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _armorBarWidth);
    }

    private void Update() {
        if (_renderer == null) return;
        if (!IsOwner) return;

        var hp = Math.Clamp(_damageable.CurrentHealth / _damageable.Health.maxHealth, 0, 1);
        _renderer.hp.transform.localScale = new Vector3(hp, 1, 1);
        var hpArmor = _damageable.CurrentHealth + _damageable.CurrentArmor;
        _renderer.hpText.text = $"{hpArmor:0}/{_damageable.Health.maxHealth:0}";

        var armor = Math.Clamp(_damageable.CurrentArmor / _damageable.Armor.maxArmor, 0, 1);
        _renderer.armor.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _armorBarWidth * armor);

        var primalMp = Math.Clamp(_caster.Mana.PrimalMana / _caster.Mana.MaxMana, 0, 1);
        _renderer.primalMp.transform.localScale = new Vector3(primalMp, 1, 1);
        var mp = Math.Clamp(_caster.Mana.Mana / _caster.Mana.MaxMana, 0, 1);
        _renderer.mp.transform.localScale = new Vector3(mp, 1, 1);
        if (_caster.Mana.PrimalMana > 0) {
            _renderer.mpText.text = $"-{_caster.Mana.PrimalMana:0}/{_caster.Mana.MaxMana:0}";
        } else {
            _renderer.mpText.text = $"{_caster.Mana.Mana:0}/{_caster.Mana.MaxMana:0}";
        }

        var stamina = Math.Clamp(_movement.stamina.Value / _movement.maxStamina, 0, 1);
        _renderer.stamina.transform.localScale = new Vector3(stamina, 1, 1);

        Show(_statusable.DurationEffects);

        _renderer.alternativeSpawn.gameObject.SetActive(_caster.alternativeSpawn);

        if (_renderer.echoContainer.childCount != _caster.EchoCount) {
            if (_renderer.echoContainer.childCount < _caster.EchoCount) {
                for (int i = _renderer.echoContainer.childCount; i < _caster.EchoCount; i++) {
                    Instantiate(echoItemPrefab, _renderer.echoContainer);
                }
            } else {
                for (int i = _renderer.echoContainer.childCount - 1; i >= _caster.EchoCount; i--) {
                    var child = _renderer.echoContainer.GetChild(i);
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void Show(List<Statusable.DurationEffect> effects) {
        int needed = 0;
        for (int i = 0; i < effects.Count; i++) {
            if (effects[i].remains > 0f) needed++;
        }

        while (_items.Count < needed) {
            _items.Add(Instantiate(effectItemPrefab, _renderer.effectsContainer));
        }

        int idx = 0;
        for (int i = 0; i < effects.Count; i++) {
            var e = effects[i];
            if (e.remains <= 0f) continue;

            var item = _items[idx++];
            item.gameObject.SetActive(true);
            item.Set(e.icon, e.remains);
        }

        for (int i = idx; i < _items.Count; i++) {
            _items[i].gameObject.SetActive(false);
        }
    }
}