using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : NetworkBehaviour {
    private PlayerUIRenderer _renderer;
    private Damageable _damageable;
    private SpellCasterPlayer _caster;
    private Statusable _statusable;

    private float _armorBarWidth;

    [SerializeField] private float barsSmoothSpeed = 10f;

    private float _displayHp = 1f;
    private float _displayArmor = 0f;
    private float _displayPrimalMp = 0f;
    private float _displayMp = 1f;
    private float _displayHpSpendPreview = 0f;
    private float _displayMpSpendPreview = 0f;

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
        var ui = GetComponentsInChildren<UICameraSway>();
        for (var i = 0; i < ui.Length; i++) {
            ui[i].Bind(_renderer.uiContainers[i]);
        }

        _armorBarWidth = _renderer.armor.rect.width;

        _displayHp = Math.Clamp(_damageable.CurrentHealth / _damageable.Health.maxHealth, 0, 1);
        _displayArmor = Math.Clamp(_damageable.CurrentArmor / _damageable.Armor.maxArmor, 0, 1);
        _displayPrimalMp = Math.Clamp(_caster.Mana.PrimalMana / _caster.Mana.MaxMana, 0, 1);
        _displayMp = Math.Clamp(_caster.Mana.Mana / _caster.Mana.MaxMana, 0, 1);
        _displayHpSpendPreview = 0f;
        _displayMpSpendPreview = 0f;

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
        _displayHp = Smooth01(_displayHp, hp);
        _renderer.hp.transform.localScale = new Vector3(_displayHp, 1, 1);
        var hpArmor = _damageable.CurrentHealth + _damageable.CurrentArmor;
        _renderer.hpText.text = $"{hpArmor:0}/{_damageable.Health.maxHealth:0}";

        var armor = Math.Clamp(_damageable.CurrentArmor / _damageable.Armor.maxArmor, 0, 1);
        _displayArmor = Smooth01(_displayArmor, armor);
        _renderer.armor.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _armorBarWidth * _displayArmor);

        var primalMp = Math.Clamp(_caster.Mana.PrimalMana / _caster.Mana.MaxMana, 0, 1);
        _displayPrimalMp = Smooth01(_displayPrimalMp, primalMp);
        _renderer.primalMp.transform.localScale = new Vector3(_displayPrimalMp, 1, 1);
        var mp = Math.Clamp(_caster.Mana.Mana / _caster.Mana.MaxMana, 0, 1);
        _displayMp = Smooth01(_displayMp, mp);
        _renderer.mp.transform.localScale = new Vector3(_displayMp, 1, 1);

        var spellCost = _caster.CostForActiveSpell();
        var manaSpend = spellCost.Mana;
        var hpSpend = spellCost.Health;

        var showManaSpend = manaSpend > 0f;
        var manaSpendNormalized = Math.Clamp(manaSpend / _caster.Mana.MaxMana, 0, 1);
        _displayMpSpendPreview = Smooth01(_displayMpSpendPreview, showManaSpend ? manaSpendNormalized : 0f);
        UpdateSpendPreviewBar(_renderer.mp, _renderer.mpSpendPreview, _displayMp, _displayMpSpendPreview);
        // _renderer.mpSpendPreview.gameObject.SetActive(showManaSpend);
        _renderer.mpSpendText.gameObject.SetActive(showManaSpend);
        _renderer.mpSpendText.text = showManaSpend ? $"-{Mathf.CeilToInt(manaSpend)}" : string.Empty;

        var showHpSpend = hpSpend > 0f;
        var hpSpendNormalized = Math.Clamp(hpSpend / _damageable.Health.maxHealth, 0, 1);
        _displayHpSpendPreview = Smooth01(_displayHpSpendPreview, showHpSpend ? hpSpendNormalized : 0f);
        UpdateSpendPreviewBar(_renderer.hp, _renderer.hpSpendPreview, _displayHp, _displayHpSpendPreview);
        // _renderer.hpSpendPreview.gameObject.SetActive(showHpSpend);
        _renderer.hpSpendText.gameObject.SetActive(showHpSpend);
        _renderer.hpSpendText.text = showHpSpend ? $"-{Mathf.CeilToInt(hpSpend)}" : string.Empty;

        if (_caster.Mana.PrimalMana > 0) {
            _renderer.mpText.text = $"-{_caster.Mana.PrimalMana:0}/{_caster.Mana.MaxMana:0}";
        } else {
            _renderer.mpText.text = $"{_caster.Mana.Mana:0}/{_caster.Mana.MaxMana:0}";
        }

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

    private float Smooth01(float current, float target) {
        var t = 1f - Mathf.Exp(-barsSmoothSpeed * Time.deltaTime);
        return Mathf.Lerp(current, target, t);
    }

    private float ToParentNormalizedX(RectTransform parent, float localX) {
        return Mathf.InverseLerp(parent.rect.xMin, parent.rect.xMax, localX);
    }

    private void UpdateSpendPreviewBar(
        Image baseBar, Image spendPreview, float currentNormalized, float spendNormalized
    ) {
        var spend = Mathf.Min(spendNormalized, currentNormalized);
        var baseRect = baseBar.rectTransform;
        var parentRect = (RectTransform)spendPreview.rectTransform.parent;
        var corners = new Vector3[4];
        baseRect.GetWorldCorners(corners);
        var barLeftLocal = parentRect.InverseTransformPoint(corners[0]).x;
        var barRightLocal = parentRect.InverseTransformPoint(corners[3]).x;
        var barLeft = ToParentNormalizedX(parentRect, barLeftLocal);
        var barRight = ToParentNormalizedX(parentRect, barRightLocal);
        var barWidth = Mathf.Max(0f, barRight - barLeft);
        var spendWidth = barWidth * (spend / Mathf.Max(currentNormalized, 0.0001f));
        var right = barRight;
        var left = Mathf.Clamp01(right - spendWidth);

        var spendRect = spendPreview.rectTransform;
        var anchorMin = spendRect.anchorMin;
        var anchorMax = spendRect.anchorMax;
        anchorMin.x = left;
        anchorMax.x = right;
        spendRect.anchorMin = anchorMin;
        spendRect.anchorMax = anchorMax;
        spendRect.offsetMin = new Vector2(0f, spendRect.offsetMin.y);
        spendRect.offsetMax = new Vector2(0f, spendRect.offsetMax.y);
    }
}