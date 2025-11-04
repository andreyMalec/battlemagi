using Unity.Netcode;
using UnityEngine;

public class WaitAndDespawn : NetworkBehaviour {
    [SerializeField] private float delay = 5f;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            Invoke(nameof(Despawn), delay);
        }
    }

    private void Despawn() {
        NetworkObject.Despawn(true);
    }
}