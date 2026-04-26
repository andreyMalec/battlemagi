using Unity.Netcode;
using UnityEngine;

public class SpellCasterNetworkBridge : NetworkBehaviour, ISpellCasterBridge {
    public NetworkVariable<float> mana = new();
    public NetworkVariable<float> primalMana = new();

    public new bool IsServer => base.IsServer;
    public new bool IsSpawned => base.IsSpawned;
    public new bool IsOwner => base.IsOwner;
    public ulong OwnerId => OwnerClientId;

    private SpellCasterPlayer _core;
    private bool _hasCore;
    private SpellDefinition _channelingSpell;
    private SpellInstance _channelingSpellInstance;
    private bool _hadChannelingSpellInstance;
    private string _channelingSpellName;
    private ulong _channelingSpellObjectId = ulong.MaxValue;
    private bool _stopChannelingRequested;

    public bool TrySpendMana(float amount) {
        if (!_hasCore) return false;
        if (amount <= 0f) return true;

        if (IsServer) {
            var spent = _core.Mana.SpendWithPrimalServer(amount);
            SyncFromCore();
            return spent;
        }

        var predicted = _core.Mana.SpendWithPrimalServer(amount);
        SpendManaServerRpc(amount);
        return predicted;
    }

    public bool TrySpendHealth(float amount) {
        if (!_hasCore) return false;
        if (amount <= 0f) return true;

        var damageable = _core.GetComponent<Damageable>();
        if (damageable == null) return false;

        if (IsServer)
            return damageable.SpendHealthCostServer(amount);

        if (!damageable.CanSpendHealthCost(amount)) return false;
        SpendHealthServerRpc(amount);
        return true;
    }

    public void RestoreEcho(SpellDefinition spell, int amount) {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;
        if (spell == null || amount <= 0) return;

        var sendParams = new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
        RestoreEchoClientRpc(spell.name, amount, sendParams);
    }

    public void BeginChanneling(SpellDefinition spell) {
        _channelingSpell = spell;
        _channelingSpellInstance = null;
        _hadChannelingSpellInstance = false;
        _channelingSpellName = spell?.name;
        _channelingSpellObjectId = ulong.MaxValue;
        _stopChannelingRequested = false;
    }

    public void EndChanneling() {
        _channelingSpell = null;
        _channelingSpellInstance = null;
        _hadChannelingSpellInstance = false;
        _channelingSpellName = null;
        _channelingSpellObjectId = ulong.MaxValue;
        _stopChannelingRequested = false;
    }

    public void RequestStopChanneling() {
        _stopChannelingRequested = true;

        if (IsServer) {
            KillChannelingSpell(_channelingSpell);
            return;
        }

        RequestStopChannelingServerRpc(_channelingSpellName);
    }

    public bool ShouldStopChanneling() {
        if (_stopChannelingRequested) return true;

        if (IsServer) {
            var active = FindChannelingSpellInstance();
            if (active != null) {
                _hadChannelingSpellInstance = true;
                return !active.IsAlive;
            }

            return _hadChannelingSpellInstance;
        }

        if (_channelingSpellObjectId == ulong.MaxValue) return false;
        return NetworkManager.Singleton == null ||
               !NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(_channelingSpellObjectId);
    }

    public void BindChannelingSpell(ulong spellObjectId, string spellName) {
        if (!_hasCore) return;
        if (!IsOwner) return;
        if (_channelingSpellName != spellName) return;

        _channelingSpellObjectId = spellObjectId;
    }

    public void StopChannelingSpell(ulong spellObjectId) {
        if (!_hasCore) return;
        if (!IsOwner) return;
        if (_channelingSpellObjectId != spellObjectId) return;

        _stopChannelingRequested = true;
        _core.StopChannelingFromBridge();
    }

    public void Bind(SpellCasterPlayer core) {
        _core = core;
        _hasCore = true;

        if (IsServer && IsSpawned) {
            _core.InitializeServerMana();
            SyncFromCore();
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!_hasCore) return;

        mana.OnValueChanged += OnManaChanged;
        primalMana.OnValueChanged += OnPrimalManaChanged;

        _core.Mana.SetNetworkState(mana.Value, primalMana.Value);

        if (IsServer) {
            _core.InitializeServerMana();
            SyncFromCore();
        }
    }

    public override void OnNetworkDespawn() {
        if (!_hasCore) return;
        mana.OnValueChanged -= OnManaChanged;
        primalMana.OnValueChanged -= OnPrimalManaChanged;
        EndChanneling();
        base.OnNetworkDespawn();
    }

    private void FixedUpdate() {
        if (!_hasCore) return;
        TickFixed(_core);
    }

    public void TickFixed(SpellCasterPlayer core) {
        if (!IsServer) return;
        if (!IsSpawned) return;
        core.TickServerMana(Time.fixedDeltaTime);
        SyncFromCore();
    }

    private void OnManaChanged(float prev, float next) {
        _core.Mana.SetNetworkState(next, _core.Mana.PrimalMana);
    }

    private void OnPrimalManaChanged(float prev, float next) {
        _core.Mana.SetNetworkState(_core.Mana.Mana, next);
    }

    private SpellInstance FindChannelingSpellInstance() {
        if (_channelingSpellInstance != null && SpellInstance.Active.Contains(_channelingSpellInstance))
            return _channelingSpellInstance;

        _channelingSpellInstance = SpellInstance.Active.Find(it =>
            it.OwnerId == _core.OwnerId && it.IsAlive && it.Bind.Context.Spell == _channelingSpell);
        return _channelingSpellInstance;
    }

    private void KillChannelingSpell(SpellDefinition spell) {
        var active = FindChannelingSpellInstance();
        if (active == null && spell != null) {
            active = SpellInstance.Active.Find(it =>
                it.OwnerId == _core.OwnerId && it.IsAlive && it.Bind.Context.Spell == spell);
        }

        if (active == null) return;

        active.Bind.Context.View.Kill(active.Bind.Context);
    }

    public void SyncFromCore() {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;
        mana.Value = _core.Mana.Mana;
        primalMana.Value = _core.Mana.PrimalMana;
        _core.Mana.SetNetworkState(mana.Value, primalMana.Value);
    }

    [ServerRpc]
    private void SpendManaServerRpc(float amount) {
        if (!_hasCore) return;
        if (!IsSpawned) return;

        _core.Mana.SpendWithPrimalServer(amount);
        SyncFromCore();
    }

    [ServerRpc]
    private void SpendHealthServerRpc(float amount) {
        if (!_hasCore) return;
        if (!IsSpawned) return;

        var damageable = _core.GetComponent<Damageable>();
        if (damageable == null) return;
        damageable.SpendHealthCostServer(amount);
    }

    [ServerRpc]
    private void RequestStopChannelingServerRpc(string spellName) {
        var spell = DefaultSpells.Get(spellName)?.spell ?? DefaultSpells.GetSubSpell(spellName);
        KillChannelingSpell(spell);
    }

    [ClientRpc]
    private void RestoreEchoClientRpc(string spellWords, int amount, ClientRpcParams clientRpcParams = default) {
        _ = clientRpcParams;
        if (!_hasCore) return;
        _core.ApplyRestoreEcho(spellWords, amount);
    }
}

