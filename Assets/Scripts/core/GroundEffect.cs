using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GroundEffect : NetworkBehaviour {
    [SerializeField] private List<StatusEffectData> effects;
    [SerializeField] private float duration = 20;
    [SerializeField] private bool oneShot = false;

    private float _tickTimer;
    private readonly List<ulong> _affectedIds = new();

    private bool _destroyed = false;

    public void Initialize(List<StatusEffectData> e, float d, bool o) {
        effects = e;
        duration = d;
        oneShot = o;
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer) return;
        if (!other.TryGetComponent<StatusEffectManager>(out var manager)) return;
        if (oneShot) {
            var netObj = other.GetComponent<NetworkObject>();
            if (_affectedIds.Contains(netObj.NetworkObjectId)) return;
            _affectedIds.Add(netObj.NetworkObjectId);
        }

        var ownerId = OwnerClientId;
        if (NetworkObject.IsSceneObject == true)
            ownerId = ulong.MaxValue;
        foreach (var effect in effects) {
            manager.AddEffect(ownerId, effect);
        }
    }

    private void Update() {
        if (_destroyed) return;
        if (duration < 0) return;

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= duration) {
            if (IsServer) {
                _destroyed = true;
                var netObj = GetComponent<NetworkObject>();
                DestroyClientRpc(netObj.NetworkObjectId);
            }
        }
    }

    [ClientRpc]
    private void DestroyClientRpc(ulong netObjId) {
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