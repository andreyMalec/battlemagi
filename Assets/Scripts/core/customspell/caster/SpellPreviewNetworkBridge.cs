using System;
using TMPro;
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
        ShowServerRpc(spell.words, OwnerId);
    }

    public void Hide() {
        HideServerRpc(OwnerId);
    }

    public void StartCharging() {
        StartChargingServerRpc(OwnerId);
    }

    [ServerRpc]
    private void StartChargingServerRpc(ulong clientId) {
        StartChargingClientRpc(clientId);
    }

    [ClientRpc]
    private void StartChargingClientRpc(ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        client.PlayerObject.GetComponentInChildren<SpellInHand>()?.StartCharging();
    }

    [ServerRpc]
    private void ShowServerRpc(string spellWords, ulong clientId) {
        ShowInHandClientRpc(spellWords, clientId);
    }

    [ClientRpc]
    private void ShowInHandClientRpc(string spellWords, ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        if (!client.PlayerObject.TryGetComponent<SpellPreviewNetworkBridge>(out var bridge)) return;

        var prefab = DefaultSpells.Get(spellWords)?.inHandPrefab;
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, bridge._hand);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    [ServerRpc]
    private void HideServerRpc(ulong clientId) {
        ClearInHandClientRpc(clientId);
    }

    [ClientRpc]
    private void ClearInHandClientRpc(ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        if (!client.PlayerObject.TryGetComponent<SpellPreviewNetworkBridge>(out var bridge)) return;

        for (int i = 0; i < bridge._hand.childCount; i++) {
            Destroy(bridge._hand.GetChild(i).gameObject);
        }
    }
}