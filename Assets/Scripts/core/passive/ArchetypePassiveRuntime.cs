using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Stats))]
[RequireComponent(typeof(Statusable))]
[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(SpellCasterPlayer))]
public class ArchetypePassiveRuntime : MonoBehaviour {
    private Stats _stats;
    private Statusable _statusable;
    private Damageable _damageable;
    private SpellCasterPlayer _caster;

    private ArchetypePassiveConfig _config;
    private bool _configured;
    private bool _subscribed;

    private readonly List<PassiveStatModifier> _baseModifiers = new();
    private float _activeSpellDamageModifier = 1f;
    private bool _activeSpellDamageModifierApplied;

    private void Awake() {
        _stats = GetComponent<Stats>();
        _statusable = GetComponent<Statusable>();
        _damageable = GetComponent<Damageable>();
        _caster = GetComponent<SpellCasterPlayer>();
    }

    private void OnDestroy() {
        Unsubscribe();
        ClearDynamicSpellDamageModifier();
        RemoveBaseModifiers();
    }

    public void Configure(ArchetypePassiveConfig config) {
        _config = config;
        _configured = true;

        ApplyBaseModifiers();
        ApplySpawnEffects();
        Subscribe();
    }

    private void FixedUpdate() {
        if (!_configured)
            return;

        UpdateActiveSpellDamageModifier();
    }

    private void Subscribe() {
        if (_subscribed)
            return;

        _subscribed = true;
        Damageable.OnModifyOutgoingDamage += ModifyOutgoingDamage;
        Damageable.OnDamageDealt += HandleDamageDealt;
        _caster.OnResourceSpentServer += HandleResourceSpent;
    }

    private void Unsubscribe() {
        if (!_subscribed)
            return;

        _subscribed = false;
        Damageable.OnModifyOutgoingDamage -= ModifyOutgoingDamage;
        Damageable.OnDamageDealt -= HandleDamageDealt;
        _caster.OnResourceSpentServer -= HandleResourceSpent;
    }

    private void ApplyBaseModifiers() {
        RemoveBaseModifiers();
        if (_config.baseStatModifiers == null)
            return;

        for (var i = 0; i < _config.baseStatModifiers.Length; i++) {
            var modifier = _config.baseStatModifiers[i];
            _stats.AddModifier(modifier.statType, modifier.multiplier);
            _baseModifiers.Add(modifier);
        }
    }

    private void RemoveBaseModifiers() {
        for (var i = 0; i < _baseModifiers.Count; i++) {
            var modifier = _baseModifiers[i];
            _stats.RemoveModifier(modifier.statType, modifier.multiplier);
        }

        _baseModifiers.Clear();
    }

    private void ApplySpawnEffects() {
        if (_config.onSpawnEffects == null)
            return;

        for (var i = 0; i < _config.onSpawnEffects.Length; i++) {
            _statusable.AddEffect(_damageable.OwnerId, _config.onSpawnEffects[i]);
        }
    }

    private float ModifyOutgoingDamage(ParticipantId fromId, Damageable target, DamageRequest request, float amount) {
        if (fromId != _damageable.OwnerId)
            return amount;

        var multiplier = _config.outgoingDamageMultiplier;
        if (_config.distanceDamageBonusPerMeter > 0f) {
            var distance = Vector3.Distance(transform.position, target.transform.position);
            var bonus = distance * _config.distanceDamageBonusPerMeter;
            if (_config.maxDistanceDamageBonus > 0f)
                bonus = Mathf.Min(bonus, _config.maxDistanceDamageBonus);
            multiplier += bonus;
        }

        return amount * multiplier;
    }

    private void HandleDamageDealt(ParticipantId fromId, Damageable target, DamageApplied applied) {
        if (fromId != _damageable.OwnerId)
            return;

        if (_config.lifeStealFraction > 0f && applied.healthApplied > 0f && fromId != target.OwnerId) {
            _damageable.TakeHeal("Archetype Lifesteal", applied.healthApplied * _config.lifeStealFraction);
        }

        if (_config.onDealDamageEffects == null)
            return;

        var targetStatusable = target.GetComponent<Statusable>();
        for (var i = 0; i < _config.onDealDamageEffects.Length; i++) {
            if (applied.request.source == _config.onDealDamageEffects[i].effectName) continue;

            targetStatusable.AddEffect(
                new StatusEffectApplyContext(_damageable.OwnerId, applied),
                _config.onDealDamageEffects[i]
            );
        }
    }

    private void HandleResourceSpent(SpellDefinition _, float amount, bool isBloodMagic) {
        if (isBloodMagic)
            return;
        if (_config.armorPerManaSpent <= 0f)
            return;

        _damageable.TakeArmor(amount * _config.armorPerManaSpent);
    }

    private void UpdateActiveSpellDamageModifier() {
        if (_config.spellDamagePerActiveOwnedSpell == 0f)
            return;

        var activeCount = 0;
        for (var i = 0; i < SpellInstance.Active.Count; i++) {
            var instance = SpellInstance.Active[i];
            if (instance == null)
                continue;
            if (!instance.IsAlive)
                continue;
            if (!instance.CanGet)
                continue;
            if (instance.OwnerId != _damageable.OwnerId)
                continue;

            activeCount++;
        }

        if (_config.spellDamageCountCap > 0)
            activeCount = Mathf.Min(activeCount, _config.spellDamageCountCap);

        var nextModifier = 1f + activeCount * _config.spellDamagePerActiveOwnedSpell;
        if (Mathf.Approximately(nextModifier, _activeSpellDamageModifier))
            return;

        ClearDynamicSpellDamageModifier();
        _activeSpellDamageModifier = nextModifier;

        if (Mathf.Approximately(_activeSpellDamageModifier, 1f))
            return;

        _stats.AddModifier(StatType.SpellDamage, _activeSpellDamageModifier);
        _activeSpellDamageModifierApplied = true;
    }

    private void ClearDynamicSpellDamageModifier() {
        if (!_activeSpellDamageModifierApplied)
            return;

        _stats.RemoveModifier(StatType.SpellDamage, _activeSpellDamageModifier);
        _activeSpellDamageModifierApplied = false;
        _activeSpellDamageModifier = 1f;
    }
}