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

    public void SetChanneling(bool active) {
        ChannelingClientRpc(OwnerClientId, active);
    }

    [ClientRpc]
    private void ChannelingClientRpc(ulong targetClientId, bool active) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client)) return;
        var player = client.PlayerObject;
        Debug.Log($"ChannelingClientRpc player={client.PlayerObject.name}, active={active}");

        var caster = player.GetComponent<PlayerSpellCaster>();
        caster.channeling = active;
        if (active) {
            var animator = player.GetComponent<PlayerAnimator>();
            animator.TriggerChanneling();
        }
    }
}