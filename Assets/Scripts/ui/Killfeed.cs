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
        var killerName = ResolveName(ParticipantIdentityCodec.Decode(killerId));
        var targetName = ResolveName(ParticipantIdentityCodec.Decode(targetId));

        var killInfo = $"{killerName} → {targetName}";

        item.GetComponent<KillfeedItem>().SetText(killInfo);
    }

    private static string ResolveName(ParticipantId id) {
        if (id == ParticipantId.EnvironmentId) return "";

        if (id.IsHuman) {
            var player = PlayerManager.Instance.FindByClientId(id.Value);
            if (!player.HasValue)
                return "";
            return Trim(player.Value.Name());
        }

        return Trim(BotNameCatalog.Resolve(id.Value));
    }

    private static string Trim(string value) {
        if (value.Length > 12)
            return $"{value[..12]}..";
        return value;
    }
}