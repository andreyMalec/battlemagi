using Unity.Netcode;
using UnityEngine;

public class LobbyEnjoyer : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        name = $"LobbyEnjoyer_{OwnerClientId}";
    }
}