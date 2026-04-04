using TMPro;
using UnityEngine;

public class SpellCasterLocalBridge : MonoBehaviour, ISpellCasterBridge {
    [SerializeField] private ulong clientId;
    [SerializeField] private TMP_Text manaText;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public bool IsOwner => clientId == 0;
    public ulong OwnerId => clientId;

    private SpellCasterPlayer _core;
    private bool _hasCore;

    public void Bind(SpellCasterPlayer core) {
        _core = core;
        _hasCore = true;
    }

    private void Update() {
        if (manaText != null) {
            if (_core.Mana.PrimalMana > 0) {
                manaText.text = $"-{_core.Mana.PrimalMana:0}({_core.Mana.Mana:0})/{_core.Mana.MaxMana:0}";
            } else {
                manaText.text = $"{_core.Mana.Mana:0}/{_core.Mana.MaxMana:0}";
            }
        }
    }

    private void FixedUpdate() {
        if (!_hasCore) return;
        TickFixed(_core);
    }

    public void TickFixed(SpellCasterPlayer core) {
        core.TickServerMana(Time.fixedDeltaTime);
    }

    public bool TrySpendMana(float amount) {
        if (!_hasCore) return false;
        return _core.Mana.SpendWithPrimalServer(amount);
    }

    public bool TrySpendHealth(float amount) {
        if (!_hasCore) return false;
        var damageable = _core.GetComponent<Damageable>();
        if (damageable == null) return false;
        return damageable.SpendHealthCostServer(amount);
    }

    public void RestoreEcho(SpellDefinition spell, int amount) {
        if (!_hasCore) return;
        _core.ApplyRestoreEcho(spell, amount);
    }
}