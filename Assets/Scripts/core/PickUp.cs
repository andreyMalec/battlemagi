using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PickUp : NetworkBehaviour {
    [SerializeField] private List<StatusEffectData> effects;

    private bool _destroyed = false;

    private void OnTriggerEnter(Collider other) {
        if (other.isTrigger) return;
        if (_destroyed) return;
        if (!IsServer) return;

        if (other.TryGetComponent<Player>(out _) && other.TryGetComponent<StatusEffectManager>(out var manager)) {
            var ownerId = OwnerClientId;
            if (NetworkObject.IsSceneObject == true)
                ownerId = PlayerId.EnvironmentId;
            foreach (var effect in effects) {
                manager.AddEffect(ownerId, effect);
            }

            var toUI = effects.First();
            if (string.IsNullOrWhiteSpace(toUI.title)) {
                OnPickupClientRpc();
            } else
                OnPickupClientRpc(
                    other.GetComponent<NetworkObject>().OwnerClientId,
                    R.String(toUI.title),
                    R.String(toUI.description),
                    toUI.color);

            // server-authoritative despawn
            DestroySelf();
        }
    }

    [ClientRpc]
    private void OnPickupClientRpc() {
        GetComponentInParent<AudioSource>().Play();
    }

    [ClientRpc]
    private void OnPickupClientRpc(ulong clientId, string effectName, string description, Color color) {
        GetComponentInParent<AudioSource>().Play();
        if (NetworkManager.LocalClientId == clientId) {
            var ui = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerEffectUI>();
            ui.Show(effectName, description, color);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyServerRpc() {
        DestroySelf();
    }

    private void DestroySelf() {
        if (_destroyed) return;
        _destroyed = true;
        if (NetworkObject != null && NetworkObject.IsSpawned) {
            NetworkObject.Despawn(true);
        } else {
            Destroy(gameObject);
        }
    }
}