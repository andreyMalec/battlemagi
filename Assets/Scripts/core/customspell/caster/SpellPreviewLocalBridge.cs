using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class SpellPreviewLocalBridge : MonoBehaviour, ISpellPreviewBridge {
    [SerializeField] private ulong clientId;

    public bool IsServer => true;
    public bool IsSpawned => true;
    public bool IsOwner => clientId == 0;
    public ulong OwnerId => clientId;

    private Transform _hand;

    public void BindHand(Transform hand) {
        _hand = hand;
    }

    public void Show(SpellDefinition spell) {
        Hide();
        var prefab = DefaultSpells.Get(spell)?.inHandPrefab;
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, _hand);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    public void Hide() {
        for (int i = 0; i < _hand.childCount; i++) {
            Destroy(_hand.GetChild(i).gameObject);
        }
    }

    public void StartCharging() {
        GetComponentInChildren<SpellInHand>()?.StartCharging();
    }
}