using Unity.Netcode;

public class ShadowStep : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        if (!NetworkManager.ConnectedClients.TryGetValue(OwnerClientId, out var client)) return;
        var player = client.PlayerObject.gameObject;
        player.transform.position = transform.position;
    }
}