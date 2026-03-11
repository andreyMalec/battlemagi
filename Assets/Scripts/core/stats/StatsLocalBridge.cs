using UnityEngine;

public class StatsLocalBridge : MonoBehaviour, IStatsBridge {
    [SerializeField] private bool _isServer = true;

    private Stats _core;
    private bool _hasCore;

    public bool IsServer => _isServer;
    public bool IsSpawned => true;

    private void FixedUpdate() {
        if (!_hasCore) return;
        TickFixed(_core);
    }

    public void Bind(Stats core) {
        _core = core;
        _hasCore = true;
    }

    public void TickFixed(Stats core) {
        if (!IsServer) return;
        SyncFromCore(core);
    }

    public void SyncFromCore(Stats core) {
    }
}