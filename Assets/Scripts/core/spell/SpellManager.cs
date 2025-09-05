using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class SpellManager : NetworkBehaviour {
    [SerializeField] private Transform spellCastPoint;
    public Transform spellInHandPoint;

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
    private void ClearInHandServerRpc(ulong clienId) {
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects) {
            var netObj = kvp.Value;
            if (netObj != null && netObj.IsSpawned && netObj.OwnerClientId == clienId) {
                if (netObj.TryGetComponent<SpellInHand>(out _)) {
                    Debug.Log($"[SpellManager] Сервер: Подчищаем эффект в руке заклинателя");
                    netObj.Despawn();
                    Destroy(netObj.gameObject);
                    break;
                }
            }
        }
    }

    [ServerRpc]
    private void SpawnInHandServerRpc(int spellId, ulong clientId) {
        var spell = SpellDatabase.Instance?.GetSpell(spellId);
        if (spell == null || spell.spellInHandPrefab == null) return;

        GameObject obj = Instantiate(spell.spellInHandPrefab, spellInHandPoint.position, spellInHandPoint.rotation);

        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null) {
            Debug.LogError("Spell prefab must have a NetworkObject component");
            Destroy(obj);
            return;
        }

        netObj.SpawnWithOwnership(clientId);
        Debug.Log($"[SpellManager] Сервер: Проявляем {spell.name} в руке заклинателя {clientId}");
    }

    [ServerRpc]
    private void SpawnMainServerRpc(int spellId, Vector3 position, Quaternion rotation) {
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

        netObj.Spawn();
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

        GameObject go = netObj.gameObject;
        if (netObj.IsSpawned) {
            netObj.Despawn();
        }

        if (go != null) {
            Destroy(go);
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