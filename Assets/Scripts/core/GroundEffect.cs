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
        if (!IsServer || other.isTrigger) return;
        if (!other.TryGetComponent<StatusEffectManager>(out var manager)) return;
        if (oneShot) {
            if (_affectedIds.Contains(manager.OwnerClientId)) return;
            _affectedIds.Add(manager.OwnerClientId);
        }

        var ownerId = OwnerClientId;
        if (NetworkObject.IsSceneObject == true)
            ownerId = PlayerId.EnvironmentId;
        foreach (var effect in effects) {
            manager.AddEffect(ownerId, effect);
        }
    }

    private void Update() {
        if (!IsServer) return;
        if (_destroyed || duration < 0) return;

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= duration) {
            _destroyed = true;
            var netObj = GetComponent<NetworkObject>();
            if (netObj.IsSpawned) {
                netObj.Despawn(true);
            }
        }
    }
}