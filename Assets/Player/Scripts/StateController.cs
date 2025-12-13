using Unity.Netcode;
using UnityEngine;

public class StateController : NetworkBehaviour {
    public void SetFreeze(bool active) {
        FreezeClientRpc(NetworkObjectId, active);
    }

    [ClientRpc]
    private void FreezeClientRpc(ulong targetNetObj, bool active) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObj, out var netObj)) return;
        Debug.Log($"FreezeClientRpc targetNetObj={netObj}, active={active}");
        var freeze = GetComponentInChildren<Freeze>(true);
        if (freeze != null) freeze.gameObject.SetActive(active);
    }

    public void Attach(ulong originClientId, bool active) {
        AttachClientRpc(originClientId, NetworkObjectId, active);
    }

    [ClientRpc]
    private void AttachClientRpc(ulong originClientId, ulong targetNetObj, bool active) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObj, out var netObj)) return;
        Debug.Log($"AttachClientRpc originClientId={originClientId}, targetNetObj={netObj}, active={active}");
        var movement = netObj.GetComponent<FirstPersonMovement>();
        if (movement != null)
            movement.enabled = !active;
        if (active) {
            var parent = NetworkManager.ConnectedClients[originClientId].PlayerObject;
            if (parent != null) {
                netObj.TrySetParent(parent);
            } else {
                netObj.TryRemoveParent();
            }
        } else {
            netObj.TryRemoveParent();
        }
    }
}