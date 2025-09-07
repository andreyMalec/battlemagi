using Unity.Netcode;
using UnityEngine;

public class SpellInHand : NetworkBehaviour {
    private Transform localAnchor;

    public override void OnNetworkSpawn() {
        // Находим своего игрока по OwnerClientId
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out var client)) {
            var manager = client.PlayerObject.GetComponent<SpellManager>();
            if (manager != null) {
                Debug.Log($"[SpellInHand] Установлена локальная позиция");
                localAnchor = manager.invocation.transform;
            }
        }
    }

    void Update() {
        if (localAnchor != null) {
            transform.position = localAnchor.position;
            transform.rotation = localAnchor.rotation;
        }
    }
}