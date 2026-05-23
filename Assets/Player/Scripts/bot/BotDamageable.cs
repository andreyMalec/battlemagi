using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class BotDamageable : NetworkBehaviour {
    private ParticipantIdentity _identity;
    private Damageable _damageable;
    private readonly List<ParticipantId> _damagedBy = new();

    private void Awake() {
        _identity = GetComponent<ParticipantIdentity>();
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
        var victim = _identity.Id;
        var attacker = damageApplied.request.fromId;
        if (!_damagedBy.Contains(damageApplied.request.fromId) && victim != attacker)
            _damagedBy.Add(damageApplied.request.fromId);
    }

    private void OnDeath(DeathInfo deathInfo) {
        var victim = _identity.Id;
        var killer = deathInfo.fromId;
        Debug.Log($"BotKilled {killer} -> {victim} with {deathInfo.source}");
        NetworkObject.TryRemoveParent();
        var enemies = _damagedBy.Where(damager =>
            TeamManager.Instance.AreEnemies(victim, damager));
        foreach (var enemy in enemies) {
            if (enemy == deathInfo.fromId)
                PlayerManager.Instance.AddKill(killer);
            else
                PlayerManager.Instance.AddAssist(enemy);
        }

        PlayerManager.Instance.AddDeath(victim);
        BotLifecycleManager.Instance?.HandleBotDeath(victim, gameObject);
        Killfeed.Instance?.HandleClientRpc(ParticipantIdentityCodec.Encode(deathInfo.fromId),
            ParticipantIdentityCodec.Encode(victim));
    }
}