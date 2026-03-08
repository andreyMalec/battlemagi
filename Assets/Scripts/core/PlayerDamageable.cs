using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class PlayerDamageable : NetworkBehaviour {
    private Damageable _damageable;
    private readonly List<ulong> _damagedBy = new();
    private readonly List<DamageInfo> _damagedBySource = new();

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _damageable.OnDeath += OnDeath;
        _damageable.OnDamageApplied += OnDamageApplied;
    }

    private void OnDamageApplied(DamageApplied damageApplied) {
        if (!_damagedBy.Contains(damageApplied.request.fromId) && OwnerClientId != damageApplied.request.fromId)
            _damagedBy.Add(damageApplied.request.fromId);
        _damagedBySource.Add(new DamageInfo { damage = damageApplied.final, source = damageApplied.request.source });
    }

    private void OnDeath(DeathInfo deathInfo) {
        NetworkObject.TryRemoveParent();
        var enemies = _damagedBy.Where(damager => TeamManager.Instance.AreEnemies(OwnerClientId, damager));
        foreach (var enemy in enemies) {
            if (enemy == deathInfo.fromId)
                PlayerManager.Instance.AddKill(deathInfo.fromId);
            else
                PlayerManager.Instance.AddAssist(enemy);
        }

        PlayerManager.Instance.AddDeath(OwnerClientId);
        PlayerSpawner.instance.HandleDeathServer(OwnerClientId);
        Killfeed.Instance?.HandleClientRpc(deathInfo.fromId, OwnerClientId);
        SendAnalytics(OwnerClientId, deathInfo.source);
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

    [Serializable]
    protected struct DamageInfo : INetworkSerializable {
        public float damage;
        public FixedString128Bytes source;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref source);
        }
    }
}