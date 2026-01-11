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
        SendAnalytics(ownerClientId, source);
    }

    private void SendAnalytics(ulong ownerClientId, string source) {
        var sendParams = new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { ownerClientId } }
        };

        var damageSource = _damagedBySource
            .GroupBy(x => x.source.ToString())
            .Select(g => new DamageInfo {
                source = g.Key,
                damage = g.Sum(x => x.damage)
            })
            .OrderByDescending(x => x.damage)
            .ToArray();
        PlayerKilledClientRpc(source, damageSource, sendParams);
    }

    [ClientRpc]
    private void PlayerKilledClientRpc(string source, DamageInfo[] damagedBy, ClientRpcParams _ = default) {
        foreach (var entry in damagedBy) {
            FirebaseAnalytic.Instance.SendEvent("PlayerDamaged", new Dictionary<string, object> {
                { "source", entry.source },
                { "damage", $"{entry.damage:0}" }
            });
        }

        FirebaseAnalytic.Instance.SendEvent("PlayerKilled", new Dictionary<string, object> {
            { "source", source }
        });
    }
}