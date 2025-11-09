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
}