using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class Killfeed : NetworkBehaviour {
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject container;

    [CanBeNull] public static Killfeed Instance;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        Instance = this;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        Instance = null;
    }

    [ClientRpc]
    public void HandleClientRpc(ulong killerId, ulong targetId, ulong sourceId = 0) {
        var item = Instantiate(itemPrefab, container.transform);
        var killer = PlayerManager.Instance.FindByClientId(killerId);
        var killerName = "";
        if (killer.HasValue) {
            killerName = killer.Value.Name();
            if (killerName.Length > 12)
                killerName = $"{killerName[..12]}..";
        }

        var target = PlayerManager.Instance.FindByClientId(targetId);
        var targetName = "";
        if (target.HasValue) {
            targetName = target.Value.Name();
            if (targetName.Length > 12)
                targetName = $"{targetName[..12]}..";
        }

        var killInfo = $"{killerName} → {targetName}";

        item.GetComponent<KillfeedItem>().SetText(killInfo);
    }
}