using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GroundEffect : NetworkBehaviour {
    [SerializeField] private List<StatusEffectData> effects;
    [SerializeField] private float duration = 20;

    private float _tickTimer;

    private void OnTriggerStay(Collider other) {
        if (other.isTrigger) return;
        if (!IsServer) return;

        if (other.TryGetComponent<StatusEffectManager>(out var manager)) {
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