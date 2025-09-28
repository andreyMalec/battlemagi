using Unity.Netcode;
using UnityEngine;

public class ActiveSpell : NetworkBehaviour {
    [HideInInspector] public Transform invocation;
    private IHandAppearance handAppearance;

    private void Awake() {
        invocation = GetComponentInChildren<MeshController>().invocation.transform;
    }

    public void PrepareSpell(SpellData spell) {
        if (!IsOwner || spell == null) return;

        Debug.Log($"[SpellManager] Подготавливаем {spell.name}");
        ShowInHandServerRpc(spell.id, OwnerClientId);
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
        manager.handAppearance = spell?.spellInHandPrefab == null
            ? new NoHandSpawn()
            : new HandSpawn();
        manager.handAppearance?.Show(manager, spell);
    }

    [ServerRpc]
    public void ClearInHandServerRpc(ulong clientId) {
        ClearInHandClientRpc(clientId);
    }

    [ClientRpc]
    private void ClearInHandClientRpc(ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var manager = client.PlayerObject.GetComponent<ActiveSpell>();

        manager.handAppearance?.Clear(manager);
    }
}