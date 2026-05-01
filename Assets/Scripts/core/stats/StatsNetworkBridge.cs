using Unity.Netcode;

public class StatsNetworkBridge : NetworkBehaviour, IStatsBridge {
    private Stats _core;
    private bool _hasCore;

    private NetworkVariable<StatSnapshot> _synced = new();

    bool IStatsBridge.IsServer => base.IsServer;
    bool IStatsBridge.IsSpawned => base.IsSpawned;

    public void Bind(Stats core) {
        _core = core;
        _hasCore = true;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!_hasCore) return;

        if (!IsServer) {
            _synced.OnValueChanged += OnSyncedChanged;
            _core.SetSnapshot(_synced.Value);
        } else {
            SyncFromCore(_core);
        }
    }

    public override void OnNetworkDespawn() {
        if (_hasCore && !IsServer)
            _synced.OnValueChanged -= OnSyncedChanged;
        base.OnNetworkDespawn();
    }

    private void OnSyncedChanged(StatSnapshot prev, StatSnapshot next) {
        _core.SetSnapshot(next);
    }


    public void SyncFromCore(Stats core) {
        if (!_hasCore) return;
        if (!IsServer) return;
        if (!IsSpawned) return;
        _synced.Value = core.GetSnapshot();
    }
}

