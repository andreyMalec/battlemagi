using UnityEngine;

public class StatsLocalBridge : MonoBehaviour, IStatsBridge {
    [SerializeField] private ulong clientId;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public bool IsOwner => clientId == 0;
    public ulong OwnerId => clientId;

    public void Bind(Stats core) {
    }


    public void SyncFromCore(Stats core) {
    }
}