using Unity.Netcode;
using UnityEngine;

public class SpellCasterNetworkBridge : NetworkBehaviour, ISpellCasterBridge {
    public NetworkVariable<float> mana = new();
    public NetworkVariable<float> primalMana = new();

    public bool IsServer => base.IsServer;
    public bool IsSpawned => base.IsSpawned;
    public bool IsOwner => base.IsOwner;
    public ulong OwnerId => OwnerClientId;

    private SpellCasterPlayer _core;
    private bool _hasCore;

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

    public void SyncFromCore() {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;
        mana.Value = _core.Mana.Mana;
        primalMana.Value = _core.Mana.PrimalMana;
        _core.Mana.SetNetworkState(mana.Value, primalMana.Value);
    }
}

