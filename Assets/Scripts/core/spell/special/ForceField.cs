using System;
using Unity.Netcode;
using UnityEngine;

public class ForceField : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        var playerObj = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject;
        if (playerObj != null && playerObj.TryGetComponent<Collider>(out var playerCollider)) {
            Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, true);
        }
    }

    private void OnCollisionEnter(Collision other) {
        BlockIncomingSpell(other.gameObject);
    }

    private void OnTriggerEnter(Collider other) {
        BlockIncomingSpell(other.gameObject);
    }

    private void BlockIncomingSpell(GameObject go) {
        if (!IsServer) return;
        if (!go.TryGetComponent<NetworkObject>(out var netObj)) return;
        if (!netObj.TryGetComponent<BaseSpell>(out var spell)) return;
        if (netObj.OwnerClientId == OwnerClientId) return;
        spell.DestroySpellServerRpc(netObj.NetworkObjectId);
    }
}