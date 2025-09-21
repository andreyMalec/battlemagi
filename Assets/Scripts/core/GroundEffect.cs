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

    private void OnTriggerStay(Collider other) {
        if (other.isTrigger) return;
        if (!IsServer) return;

        if (other.TryGetComponent<StatusEffectManager>(out var manager)) {
            if (oneShot) {
                var netObj = other.GetComponent<NetworkObject>();
                if (_affectedIds.Contains(netObj.NetworkObjectId)) return;
                _affectedIds.Add(netObj.NetworkObjectId);
            }

            foreach (var effect in effects) {
                manager.AddEffect(effect);
            }
        }
    }

    private void Update() {
        if (duration < 0) return;

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= duration) {
            var netObj = GetComponent<NetworkObject>();
            if (IsServer)
                netObj.Despawn();
            Destroy(netObj.gameObject);
        }
    }
}