using UnityEngine;

public class StatsLocalBridge : MonoBehaviour, IStatsBridge {
    [SerializeField] private ulong clientId;

    private Stats _core;
    private bool _hasCore;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public bool IsOwner => clientId == 0;
    public ulong OwnerId => clientId;

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