using System;
using Unity.Netcode;
using UnityEngine;

public class ActiveSpell : NetworkBehaviour {
    [HideInInspector] public SpellManager spellManager;
    [HideInInspector] public Transform invocation;
    private IHandAppearance _handAppearance;
    private ISpellPreview _spellPreview;

    private void Awake() {
        invocation = GetComponentInChildren<MeshController>().invocation.transform;
        spellManager = GetComponent<SpellManager>();
    }

    public void PrepareSpell(SpellData spell, ISpawnStrategy spawnMode) {
        if (!IsOwner || spell == null) return;

        Debug.Log($"[SpellManager] Подготавливаем {spell.name}");
        _spellPreview = spell.previewMainInHand ? new MeshPreview() : new NoPreview();
        _spellPreview.Show(this, spawnMode, spell);
        ShowInHandServerRpc(spell.id, OwnerClientId);
    }

    private void Update() {
        _spellPreview?.Update(this);
    }

    [ServerRpc]
    private void ShowInHandServerRpc(int spellId, ulong clientId) {
        ShowInHandClientRpc(spellId, clientId);
    }

    [ClientRpc]
    private void ShowInHandClientRpc(int spellId, ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var manager = client.PlayerObject.GetComponent<ActiveSpell>();

        var spell = SpellDatabase.Instance?.GetSpell(spellId);
        manager._handAppearance = spell?.spellInHandPrefab == null
            ? new NoHandSpawn()
            : new HandSpawn();
        manager._handAppearance?.Show(manager, spell);
    }

    public void Clear() {
        _spellPreview.Clear(this);
        ClearServerRpc(OwnerClientId);
    }

    [ServerRpc]
    private void ClearServerRpc(ulong clientId) {
        ClearInHandClientRpc(clientId);
    }

    [ClientRpc]
    private void ClearInHandClientRpc(ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var manager = client.PlayerObject.GetComponent<ActiveSpell>();

        manager._handAppearance?.Clear(manager);
    }
}