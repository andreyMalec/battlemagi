using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(StatusEffectManager))]
public class PlayerDamageable : Damageable {
    public override bool IsStructure() {
        return false;
    }

    protected override void OnDeath(ulong ownerClientId, ulong fromClientId, string source) {
        NetworkObject.TryRemoveParent();
        foreach (var enemy in _damagedBy.Where(damager => TeamManager.Instance.AreEnemies(ownerClientId, damager))) {
            if (enemy == fromClientId)
                PlayerManager.Instance.AddKill(fromClientId);
            else
                PlayerManager.Instance.AddAssist(enemy);
        }

        PlayerManager.Instance.AddDeath(ownerClientId);
        PlayerSpawner.instance.HandleDeathServerRpc(ownerClientId);
        Killfeed.Instance?.HandleClientRpc(fromClientId, ownerClientId);
        var sendParams = new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { ownerClientId } }
        };
        PlayerKilledClientRpc(source, sendParams);
    }

    [ClientRpc]
    private void PlayerKilledClientRpc(string source, ClientRpcParams _ = default) {
        FirebaseAnalytic.Instance.SendEvent("PlayerKilled", new Dictionary<string, object> {
            { "source", source },
        });
    }
}