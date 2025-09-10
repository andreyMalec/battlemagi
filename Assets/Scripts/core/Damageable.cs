using Unity.Netcode;
using UnityEngine;

public class Damageable : NetworkBehaviour {
    [SerializeField] private float maxHealth = 100f;

    private NetworkVariable<float> health = new();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer)
            health.Value = maxHealth;
    }

    public void TakeDamage(float damage) {
        if (!IsServer) return;
        if (damage < 0) return;
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj != null && netObj.IsSpawned) {
            var clientId = netObj.OwnerClientId;
            Debug.Log($"[Damageable] Игрок {clientId} получает урон: {damage}");
            health.Value -= damage;

            if (health.Value <= 0) {
                PlayerSpawner.instance.HandleDeathServerRpc(clientId);
            }
        }
    }
}