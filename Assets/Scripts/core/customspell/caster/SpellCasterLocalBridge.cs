using TMPro;
using UnityEngine;

public class SpellCasterLocalBridge : MonoBehaviour, ISpellCasterBridge {
    [SerializeField] private ulong clientId;
    [SerializeField] private TMP_Text manaText;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public bool IsOwner => clientId == 0;
    public ParticipantId OwnerId {
        get => ParticipantId.Human(clientId);
        set => throw new System.NotImplementedException();
    }

    private SpellCasterPlayer _core;
    private bool _hasCore;
    private SpellDefinition _channelingSpell;
    private SpellInstance _channelingSpellInstance;
    private bool _hadChannelingSpellInstance;
    private bool _stopChannelingRequested;

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
        var spent = _core.Mana.SpendWithPrimalServer(amount);
        return spent;
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

    public void BeginChanneling(SpellDefinition spell) {
        _channelingSpell = spell;
        _channelingSpellInstance = null;
        _hadChannelingSpellInstance = false;
        _stopChannelingRequested = false;
    }

    public void EndChanneling() {
        _channelingSpell = null;
        _channelingSpellInstance = null;
        _hadChannelingSpellInstance = false;
        _stopChannelingRequested = false;
    }

    public void RequestStopChanneling() {
        _stopChannelingRequested = true;

        var active = FindChannelingSpellInstance();
        if (active == null) return;

        active.Bind.Context.View.Kill(active.Bind.Context);
    }

    public bool ShouldStopChanneling() {
        if (_stopChannelingRequested) return true;

        var active = FindChannelingSpellInstance();
        if (active != null) {
            _hadChannelingSpellInstance = true;
            return !active.IsAlive;
        }

        return _hadChannelingSpellInstance;
    }

    public void BindChannelingSpell(ulong spellObjectId, string spellName) {
    }

    public void StopChannelingSpell(ulong spellObjectId) {
    }

    private SpellInstance FindChannelingSpellInstance() {
        if (_channelingSpellInstance != null && SpellInstance.Active.Contains(_channelingSpellInstance))
            return _channelingSpellInstance;

        _channelingSpellInstance = SpellInstance.Active.Find(it =>
            it.OwnerId == _core.OwnerId && it.IsAlive && it.Bind.Context.Spell == _channelingSpell);
        return _channelingSpellInstance;
    }
}