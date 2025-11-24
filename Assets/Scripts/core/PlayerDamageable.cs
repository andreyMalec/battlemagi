using System.Linq;
using UnityEngine;

[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(StatusEffectManager))]
public class PlayerDamageable : Damageable {
    public override bool IsStructure() {
        return false;
    }

    protected override void OnDeath(ulong ownerClientId, ulong fromClientId) {
        foreach (var enemy in _damagedBy.Where(damager => TeamManager.Instance.AreEnemies(ownerClientId, damager))) {
            if (enemy == fromClientId)
                PlayerManager.Instance.AddKill(fromClientId);
            else
                PlayerManager.Instance.AddAssist(enemy);
        }

        PlayerManager.Instance.AddDeath(ownerClientId);
        PlayerSpawner.instance.HandleDeathServerRpc(ownerClientId);
        Killfeed.Instance?.HandleClientRpc(fromClientId, ownerClientId);
    }
}