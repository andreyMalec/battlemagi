using UnityEngine;

public interface IStatsBridge {
    bool IsServer { get; }
    bool IsSpawned { get; }

    void Bind(Stats core);
    void TickFixed(Stats core);
    void SyncFromCore(Stats core);
}
