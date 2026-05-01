using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class PlayerDamageable : NetworkBehaviour {
    private Damageable _damageable;
    private ParticipantIdentity _participantIdentity;
    private readonly List<ulong> _damagedBy = new();
    private readonly List<DamageInfo> _damagedBySource = new();

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _participantIdentity = GetComponent<ParticipantIdentity>();
        _damageable.OnDeath += OnDeath;
        _damageable.OnDamageApplied += OnDamageApplied;
    }

    private void OnDamageApplied(DamageApplied damageApplied) {
        var victim = GetVictimParticipantId();
        var attacker = ParticipantOwnerCodec.Decode(damageApplied.request.fromId);
        if (!_damagedBy.Contains(damageApplied.request.fromId) && victim != attacker)
            _damagedBy.Add(damageApplied.request.fromId);
        _damagedBySource.Add(new DamageInfo { damage = damageApplied.final, source = damageApplied.request.source });
    }

    private void OnDeath(DeathInfo deathInfo) {
        var victim = GetVictimParticipantId();
        var killer = ParticipantOwnerCodec.Decode(deathInfo.fromId);
        NetworkObject.TryRemoveParent();
        var enemies = _damagedBy.Where(damager => TeamManager.Instance.AreEnemies(victim, ParticipantOwnerCodec.Decode(damager)));
        foreach (var enemy in enemies) {
            if (enemy == deathInfo.fromId)
                PlayerManager.Instance.AddKill(killer);
            else
                PlayerManager.Instance.AddAssist(ParticipantOwnerCodec.Decode(enemy));
        }

        PlayerManager.Instance.AddDeath(victim);
        if (victim.IsHuman)
            PlayerSpawner.instance.HandleDeathServer(victim.Value);
        else
            Destroy(gameObject);
        Killfeed.Instance?.HandleClientRpc(deathInfo.fromId, ParticipantOwnerCodec.Encode(victim));
        if (victim.IsHuman)
            SendAnalytics(victim.Value, deathInfo.source);
    }

    private ParticipantId GetVictimParticipantId() {
        if (_participantIdentity != null)
            return _participantIdentity.Id;
        return ParticipantId.Human(OwnerClientId);
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