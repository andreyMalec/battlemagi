using Unity.Netcode;
using UnityEngine;

public class DisableComponents : NetworkBehaviour {
    [SerializeField] private Behaviour[] toDisable;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) {
            foreach (var component in toDisable) {
                component.enabled = false;
            }
        }
    }
}