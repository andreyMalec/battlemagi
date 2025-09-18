using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class SpellManager : NetworkBehaviour {
    [SerializeField] private Transform spellCastPoint;
    [HideInInspector]
    public Transform invocation;

    private void Awake() {
        invocation = GetComponentInChildren<MeshController>().invocation.transform;
    }

    public void PrepareSpell(SpellData spell) {
        if (!IsOwner) return;
        if (spell == null) return;

        Debug.Log($"[SpellManager] Заклинание {spell.name} подготавливается");
        SpawnInHandServerRpc(spell.id, NetworkManager.Singleton.LocalClientId);
    }

    public IEnumerator CastSpell(SpellData currentSpell) {
        if (!Application.isPlaying) yield break;
        if (currentSpell == null) yield break;

        yield return new WaitForSeconds(currentSpell.castTime);

        if (IsOwner) {
            ClearInHandServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        Vector3 spawnPosition = spellCastPoint != null ? spellCastPoint.position : transform.position;
        Quaternion spawnRotation = spellCastPoint != null ? spellCastPoint.rotation : transform.rotation;

        if (currentSpell.spawnOnGround) {
            spawnPosition = GetGroundPosition(spawnPosition);
        }

        SpawnMainServerRpc(currentSpell.id, spawnPosition, spawnRotation);
    }

    [ServerRpc]
    private void ClearInHandServerRpc(ulong clientId) {
        ClearInHandClientRpc(clientId);
    }

    [ClientRpc]
    private void ClearInHandClientRpc(ulong clientId) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) {
            var player = client.PlayerObject;
            var manager = player.GetComponent<SpellManager>();
            for (int i = 0; i < manager.invocation.childCount; i++) {
                Destroy(manager.invocation.GetChild(i).gameObject);
            }
            Debug.Log($"[SpellManager] Подчищаем эффект в руке заклинателя");
        }
    }

    [ServerRpc]
    private void SpawnInHandServerRpc(int spellId, ulong clientId) {
        SpawnInHandClientRpc(spellId, clientId);
    }

    [ClientRpc]
    private void SpawnInHandClientRpc(int spellId, ulong clientId) {
        var spell = SpellDatabase.Instance?.GetSpell(spellId);
        if (spell == null || spell.spellInHandPrefab == null) return;

        GameObject obj = Instantiate(spell.spellInHandPrefab, Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(invocation.transform);
        obj.transform.localPosition = Vector3.zero;

        Debug.Log($"[SpellManager] Проявляем {spell.name} в руке заклинателя {clientId}");
    }

    [ServerRpc]
    private void SpawnMainServerRpc(
        int spellId, Vector3 position,
        Quaternion rotation,
        ServerRpcParams serverRpcParams = default
    ) {
        var spell = SpellDatabase.Instance != null ? SpellDatabase.Instance.GetSpell(spellId) : null;
        if (spell == null || spell.mainSpellPrefab == null)
            return;

        GameObject obj = Instantiate(spell.mainSpellPrefab, position, rotation);
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null) {
            Debug.LogError("Main spell prefab must have a NetworkObject component");
            Destroy(obj);
            return;
        }

        netObj.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
        StartCoroutine(DespawnAndDestroyServer(netObj, spell.lifeTime));
        SpawnMainClientRpc(netObj.NetworkObjectId, spellId);
    }

    [ClientRpc]
    private void SpawnMainClientRpc(ulong objectId, int spellId, ClientRpcParams clientRpcParams = default) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;

        var spell = SpellDatabase.Instance != null ? SpellDatabase.Instance.GetSpell(spellId) : null;

        if (netObj.TryGetComponent<SpellProjectile>(out var projectile) && spell != null) {
            projectile.Initialize(spell);
        }

        netObj.gameObject.SetActive(true);
        PlayParticleSystem(netObj.gameObject);
    }

    private IEnumerator DespawnAndDestroyServer(NetworkObject netObj, float lifetime) {
        if (netObj == null) yield break;

        yield return new WaitForSeconds(lifetime);

        if (!IsServer) yield break;

        if (netObj.IsSpawned) {
            netObj.Despawn();

            GameObject go = netObj.gameObject;
            if (go != null) {
                Destroy(go);
            }
        }
    }

    private Vector3 GetGroundPosition(Vector3 originPos) {
        RaycastHit hit;
        Vector3 rayStart = originPos + Vector3.up * 10f;
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        int terrainLayerMask = 1 << terrainLayer;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, terrainLayerMask)) {
            return hit.point;
        }

        return originPos;
    }

    private void PlayParticleSystem(GameObject obj) {
        if (obj == null) return;
        ParticleSystem[] psArray = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in psArray) {
            ps.Play();
        }
    }
}