using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Сервер управляет множителями статов.
/// Клиенты автоматически получают обновления или могут запросить вручную.
/// </summary>
public class NetworkStatSystem : NetworkBehaviour {
    public StatSystem Stats { get; private set; } = new();

    private NetworkVariable<StatSnapshot> syncedStats = new();

    private void Awake() {
        Stats.OnChanged += OnStatChangedServer;
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            syncedStats.OnValueChanged += (_, snapshot) => {
                Stats.ClearAll();
                foreach (var kv in snapshot.ToDictionary())
                    Stats.AddModifier(kv.Key, kv.Value);
            };
        }
    }

    private void OnStatChangedServer(StatType _, float __) {
        if (!IsServer) return;
        syncedStats.Value = new StatSnapshot(Stats.GetAllFinals());
    }

    public void AddModifierServer(StatType type, float multiplier) {
        if (!IsServer) return;
        Stats.AddModifier(type, multiplier);
    }

    public void RemoveModifierServer(StatType type, float multiplier) {
        if (!IsServer) return;
        Stats.RemoveModifier(type, multiplier);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStatsServerRpc(ServerRpcParams rpcParams = default) {
        if (!IsServer) return;
        SendStatsClientRpc(new StatSnapshot(Stats.GetAllFinals()), rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void SendStatsClientRpc(StatSnapshot snapshot, ulong targetClientId) {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        Stats.ClearAll();
        foreach (var kv in snapshot.ToDictionary())
            Stats.AddModifier(kv.Key, kv.Value);
    }
}