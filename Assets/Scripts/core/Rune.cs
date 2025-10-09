using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Rune : NetworkBehaviour {
    [SerializeField] private List<StatusEffectData> effects;

    private bool _destroyed = false;

    private void OnTriggerEnter(Collider other) {
        if (other.isTrigger) return;
        if (_destroyed) return;
        if (!IsServer) return;

        if (other.TryGetComponent<StatusEffectManager>(out var manager)) {
            var ownerId = OwnerClientId;
            if (IsOwnedByServer)
                ownerId = ulong.MaxValue;
            foreach (var effect in effects) {
                manager.AddEffect(ownerId, effect);
            }

            SoundClientRpc();
            _destroyed = true;
            var netObj = GetComponent<NetworkObject>();
            DestroyClientRpc(netObj.NetworkObjectId);
        }
    }

    [ClientRpc]
    private void SoundClientRpc() {
        GetComponentInParent<AudioSource>().Play();
    }

    [ClientRpc]
    public void DestroyClientRpc(ulong netObjId) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out var netObj)) {
            if (IsServer && netObj.IsSpawned) {
                netObj.Despawn();
            }

            if (netObj.gameObject != null) {
                Destroy(netObj.gameObject);
            }
        }
    }
}