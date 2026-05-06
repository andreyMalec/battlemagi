using Unity.Netcode;
using UnityEngine;

public class SpellPreviewNetworkBridge : NetworkBehaviour, ISpellPreviewBridge {
    public ulong OwnerId => OwnerClientId;

    private Transform _hand;
    private ParticipantIdentity _participantIdentity;

    private void Awake() {
        _participantIdentity = GetComponent<ParticipantIdentity>();
    }

    public void BindHand(Transform hand) {
        _hand = hand;
    }

    public void Show(SpellDefinition spell) {
        Hide();
        ShowServerRpc(spell.name, GetPreviewOwnerId());
    }

    public void Hide() {
        HideServerRpc(GetPreviewOwnerId());
    }

    public void StartCharging() {
        StartChargingServerRpc(GetPreviewOwnerId());
    }

    [ServerRpc]
    private void StartChargingServerRpc(ulong ownerId) {
        StartChargingClientRpc(ownerId);
    }

    [ClientRpc]
    private void StartChargingClientRpc(ulong ownerId) {
        if (!TryResolveBridge(ownerId, out var bridge)) return;
        bridge.GetComponentInChildren<SpellInHand>()?.StartCharging();
    }

    [ServerRpc]
    private void ShowServerRpc(string spellName, ulong ownerId) {
        ShowInHandClientRpc(spellName, ownerId);
    }

    [ClientRpc]
    private void ShowInHandClientRpc(string spellName, ulong ownerId) {
        if (!TryResolveBridge(ownerId, out var bridge)) return;

        var prefab = DefaultSpells.Get(spellName)?.inHandPrefab;
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, bridge._hand);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    [ServerRpc]
    private void HideServerRpc(ulong ownerId) {
        ClearInHandClientRpc(ownerId);
    }

    [ClientRpc]
    private void ClearInHandClientRpc(ulong ownerId) {
        if (!TryResolveBridge(ownerId, out var bridge)) return;

        for (int i = 0; i < bridge._hand.childCount; i++) {
            Destroy(bridge._hand.GetChild(i).gameObject);
        }
    }

    private ulong GetPreviewOwnerId() {
        if (_participantIdentity == null)
            return OwnerId;

        return ParticipantOwnerCodec.Encode(_participantIdentity.Id);
    }

    private static bool TryResolveBridge(ulong ownerId, out SpellPreviewNetworkBridge bridge) {
        bridge = null;

        var participantId = ParticipantOwnerCodec.Decode(ownerId);
        if (participantId.IsHuman && NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(participantId.Value, out var client) &&
            client.PlayerObject != null && client.PlayerObject.TryGetComponent(out bridge))
            return true;

        var participants = FindObjectsByType<ParticipantIdentity>(FindObjectsSortMode.None);
        for (int i = 0; i < participants.Length; i++) {
            var participant = participants[i];
            if (participant.Id != participantId)
                continue;
            if (!participant.TryGetComponent(out bridge))
                continue;
            return true;
        }

        return false;
    }
}