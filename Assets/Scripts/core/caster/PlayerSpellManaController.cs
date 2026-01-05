using Unity.Netcode;
using UnityEngine;

public class PlayerSpellManaController {
    private readonly NetworkVariable<float> _mana;
    private readonly NetworkVariable<float> _primalMana;
    private readonly float _maxMana;
    private readonly float _tickInterval;
    private readonly float _manaRestore;
    private readonly NetworkStatSystem _statSystem;

    private float _restoreTick;

    public PlayerSpellManaController(
        NetworkVariable<float> mana,
        NetworkVariable<float> primalMana,
        float maxMana,
        float tickInterval,
        float manaRestore,
        NetworkStatSystem statSystem
    ) {
        _mana = mana;
        _primalMana = primalMana;
        _maxMana = maxMana;
        _tickInterval = tickInterval;
        _manaRestore = manaRestore;
        _statSystem = statSystem;
    }

    public void ServerTick(float dt) {
        _restoreTick += dt;
        if (_restoreTick >= _tickInterval) {
            var restore = _manaRestore * _statSystem.Stats.GetFinal(StatType.ManaRegen);
            _primalMana.Value -= restore;
            _mana.Value += restore;
            _restoreTick = 0f;
        }

        _mana.Value = Mathf.Clamp(_mana.Value, 0, _maxMana);
        _primalMana.Value = Mathf.Clamp(_primalMana.Value, 0f, _maxMana);
    }

    public bool CanSpendForCast(SpellData spell, int echoCount) {
        if (echoCount < spell.echoCount)
            return true;

        return _mana.Value >= CostForCast(spell);
    }

    public bool CanSpendForChannelTick(SpellData spell, float dt) {
        return _mana.Value >= CostPerSecond(spell) * dt;
    }

    public float CostForCast(SpellData spell) {
        if (spell.isChanneling)
            return CostPerSecond(spell) * _tickInterval;

        return CostPerSecond(spell);
    }

    public float CostPerSecond(SpellData spell) {
        return spell.manaCost * _statSystem.Stats.GetFinal(StatType.ManaCost);
    }
}