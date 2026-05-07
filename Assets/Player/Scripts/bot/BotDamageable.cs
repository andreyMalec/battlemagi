using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class BotDamageable : NetworkBehaviour {
    private Damageable _damageable;
    private readonly List<ulong> _damagedBy = new();

    private void Awake() {
        _damageable = GetComponent<Damageable>();
        _damageable.OnDamageApplied += OnDamageApplied;
        _damageable.OnDeath += OnDeath;
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (_damageable == null)
            return;

        _damageable.OnDamageApplied -= OnDamageApplied;
        _damageable.OnDeath -= OnDeath;
    }

    private void OnDamageApplied(DamageApplied damageApplied) {
        var victim = GetComponent<ParticipantIdentity>().Id;
        var attacker = ParticipantOwnerCodec.Decode(damageApplied.request.fromId);
        if (!_damagedBy.Contains(damageApplied.request.fromId) && victim != attacker)
            _damagedBy.Add(damageApplied.request.fromId);
    }

    private void OnDeath(DeathInfo deathInfo) {
        var victim = GetComponent<ParticipantIdentity>().Id;
        var killer = ParticipantOwnerCodec.Decode(deathInfo.fromId);
        NetworkObject.TryRemoveParent();
        var enemies = _damagedBy.Where(damager =>
            TeamManager.Instance.AreEnemies(victim, ParticipantOwnerCodec.Decode(damager)));
        foreach (var enemy in enemies) {
            if (enemy == deathInfo.fromId)
                PlayerManager.Instance.AddKill(killer);
            else
                PlayerManager.Instance.AddAssist(ParticipantOwnerCodec.Decode(enemy));
        }

        PlayerManager.Instance.AddDeath(victim);
        BotLifecycleManager.Instance?.HandleBotDeath(victim, gameObject);
        Killfeed.Instance?.HandleClientRpc(deathInfo.fromId, ParticipantOwnerCodec.Encode(victim));
    }
}