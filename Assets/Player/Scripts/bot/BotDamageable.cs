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
        _damageable.OnDeath += OnDeath;
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