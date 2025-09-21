using Unity.Netcode;
using UnityEngine;

public class StateController : NetworkBehaviour {
    public void SetFreeze(bool active) {
        FreezeClientRpc(NetworkObjectId, active);
    }

    [ClientRpc]
    private void FreezeClientRpc(ulong targetNetObj, bool active) {
        Debug.Log($"FreezeClientRpc targetNetObj={targetNetObj}, active={active}");
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObj, out var netObj)) return;
        Debug.Log($"FreezeClientRpc netObj={netObj}");
        var freeze = GetComponentInChildren<Freeze>(true);
        if (freeze != null) freeze.gameObject.SetActive(active);
    }
}