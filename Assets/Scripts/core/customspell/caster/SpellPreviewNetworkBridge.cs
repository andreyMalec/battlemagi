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
        var prefab = spell.coreType switch {
            CoreType.Projectile => (int)spell.projectile.prefabId,
            CoreType.Zone => (int)spell.zone.prefabId,
            CoreType.Beam => (int)spell.beam.prefabId,
            CoreType.Summon => (int)spell.summon.prefabId,
            _ => -1
        };
        ShowServerRpc((int)spell.coreType, prefab, OwnerId);
    }

    public void Hide() {
        HideServerRpc(OwnerId);
    }

    [ServerRpc]
    private void ShowServerRpc(int spellCore, int spellPrefab, ulong clientId) {
        ShowInHandClientRpc(spellCore, spellPrefab, clientId);
    }

    [ClientRpc]
    private void ShowInHandClientRpc(int spellCore, int spellPrefab, ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        if (!client.PlayerObject.TryGetComponent<SpellPreviewNetworkBridge>(out var bridge)) return;

        var prefab = SpellPrefabDatabase.Instance.Hand(spellCore, spellPrefab);
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