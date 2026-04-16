public interface IStatsBridge {
    bool IsServer { get; }
    bool IsSpawned { get; }

    void Bind(Stats core);
    void SyncFromCore(Stats core);
}
