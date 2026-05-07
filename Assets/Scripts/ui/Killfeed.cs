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
        Instance = null;
        base.OnNetworkDespawn();
    }

    [ClientRpc]
    public void HandleClientRpc(ulong killerId, ulong targetId, ulong sourceId = 0) {
        var item = Instantiate(itemPrefab, container.transform);
        var killerName = ResolveName(killerId);
        var targetName = ResolveName(targetId);

        var killInfo = $"{killerName} → {targetName}";

        item.GetComponent<KillfeedItem>().SetText(killInfo);
    }

    private static string ResolveName(ulong rawId) {
        if (rawId == ulong.MaxValue) return "";

        var participantId = ParticipantOwnerCodec.Decode(rawId);
        if (participantId.IsHuman) {
            var player = PlayerManager.Instance.FindByClientId(participantId.Value);
            if (!player.HasValue)
                return "";
            return Trim(player.Value.Name());
        }

        return Trim($"Bot_{participantId.Value}");
    }

    private static string Trim(string value) {
        if (value.Length > 12)
            return $"{value[..12]}..";
        return value;
    }
}