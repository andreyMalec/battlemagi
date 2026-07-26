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
            Ctx.AreEnemies(victim, damager));
        foreach (var enemy in enemies) {
            if (enemy == deathInfo.fromId)
                Ctx.Players.AddKill(killer);
            else
                Ctx.Players.AddAssist(enemy);
        }

        if (!FallController.TryApplyPhysicsKillCredit(_identity.Id, deathInfo)) {
            Ctx.Killfeed?.HandleClientRpc(ParticipantIdentityCodec.Encode(deathInfo.fromId),
                ParticipantIdentityCodec.Encode(victim));
        }

        Ctx.Players.AddDeath(victim);
        Ctx.BotLifecycle?.HandleBotDeath(victim, gameObject);
    }
}