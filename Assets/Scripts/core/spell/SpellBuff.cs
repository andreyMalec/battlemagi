using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SpellBuff : BaseSpell {
    [SerializeField] private StatusEffectData[] effects;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        var player = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject;
        if (player == null || !player.TryGetComponent<StatusEffectManager>(out var manager)) return;

        foreach (var effect in effects) {
            manager.AddEffect(OwnerClientId, effect);
        }
    }
}