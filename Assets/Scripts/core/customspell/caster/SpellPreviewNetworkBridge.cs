using Unity.Netcode;
using UnityEngine;

public class SpellPreviewNetworkBridge : NetworkBehaviour, ISpellPreviewBridge {
    public ulong OwnerId => OwnerClientId;

    private Transform _hand;

    public void BindHand(Transform hand) {
        _hand = hand;
    }

    public void Show(SpellDefinition spell) {
        Hide();
        ShowServerRpc(spell.name, NetworkObjectId);
    }

    public void Hide() {
        HideServerRpc(NetworkObjectId);
    }

    public void StartCharging() {
        StartChargingServerRpc(NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartChargingServerRpc(ulong previewObjectId) {
        StartChargingClientRpc(previewObjectId);
    }

    [ClientRpc]
    private void StartChargingClientRpc(ulong previewObjectId) {
        if (!TryResolveBridge(previewObjectId, out var bridge)) return;
        bridge.GetComponentInChildren<SpellInHand>()?.StartCharging();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShowServerRpc(string spellName, ulong previewObjectId) {
        ShowInHandClientRpc(spellName, previewObjectId);
    }

    [ClientRpc]
    private void ShowInHandClientRpc(string spellName, ulong previewObjectId) {
        if (!TryResolveBridge(previewObjectId, out var bridge)) return;

        var prefab = DefaultSpells.Get(spellName)?.inHandPrefab;
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, bridge._hand);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideServerRpc(ulong previewObjectId) {
        ClearInHandClientRpc(previewObjectId);
    }

    [ClientRpc]
    private void ClearInHandClientRpc(ulong previewObjectId) {
        if (!TryResolveBridge(previewObjectId, out var bridge)) return;

        for (int i = 0; i < bridge._hand.childCount; i++) {
            Destroy(bridge._hand.GetChild(i).gameObject);
        }
    }

    private static bool TryResolveBridge(ulong previewObjectId, out SpellPreviewNetworkBridge bridge) {
        bridge = null;
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.SpawnManager == null)
            return false;
        if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(previewObjectId, out var networkObject))
            return false;
        return networkObject.TryGetComponent(out bridge);
    }
}