using System;
using UnityEngine;

public class Stats : MonoBehaviour {
    [SerializeField] private MonoBehaviour _bridge;

    public StatSystem System { get; private set; } = new();

    private IStatsBridge _bridgeTyped;

    private void Awake() {
        if (_bridge != null)
            _bridgeTyped = (IStatsBridge)_bridge;
        else
            _bridgeTyped = GetComponentInParent<IStatsBridge>();
        _bridgeTyped.Bind(this);

        System.OnChanged += OnChangedServer;
    }

    private void OnDestroy() {
        System.OnChanged -= OnChangedServer;
    }

    public float GetFinal(StatType type) => System.GetFinal(type);

    public void AddModifier(StatType type, float multiplier) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        System.AddModifier(type, multiplier);
        _bridgeTyped.SyncFromCore(this);
    }

    public void RemoveModifier(StatType type, float multiplier) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        System.RemoveModifier(type, multiplier);
        _bridgeTyped.SyncFromCore(this);
    }

    internal void SetSnapshot(StatSnapshot snapshot) {
        System.ClearAll();
        foreach (var kv in snapshot.ToDictionary())
            System.AddModifier(kv.Key, kv.Value);
    }

    internal StatSnapshot GetSnapshot() {
        return new StatSnapshot(System.GetAllFinals());
    }

    private void OnChangedServer(StatType _, float __) {
        if (!_bridgeTyped.IsServer) return;
        if (!_bridgeTyped.IsSpawned) return;
        _bridgeTyped.SyncFromCore(this);
    }
}

