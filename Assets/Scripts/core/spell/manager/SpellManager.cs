using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class SpellManager : NetworkBehaviour {
    [SerializeField] public Transform spellCastPoint;
    [SerializeField] private ActiveSpell activeSpell;
    [HideInInspector] public NetworkStatSystem statSystem;

    private MeshController _meshController;
    private ISpawnStrategy spawnStrategy;
    private SpellData spellData;

    private void Awake() {
        if (!TryGetComponent(out activeSpell))
            activeSpell = gameObject.AddComponent<ActiveSpell>();
        _meshController = GetComponentInChildren<MeshController>();
        statSystem = GetComponent<NetworkStatSystem>();
    }

    public override void OnNetworkSpawn() {
        _meshController.OnCast += OnSpellCasted;
        _meshController.OnBurst += OnBurst;
    }

    public override void OnNetworkDespawn() {
        _meshController.OnCast -= OnSpellCasted;
        _meshController.OnBurst -= OnBurst;
    }

    public void PrepareSpell(SpellData spell) {
        if (!IsOwner || spell == null) return;

        spawnStrategy = spell.spawnMode switch {
            SpawnMode.Arc => new ArcSpawn(15f, spell.multiProjDelay),
            SpawnMode.GroundPoint => new GroundPointSpawn(spell.multiProjDelay),
            _ => new DirectSpawn(spell.multiProjDelay),
        };
        activeSpell.PrepareSpell(spell);
    }

    public void CancelSpell() {
        if (IsOwner) {
            activeSpell.ClearInHandServerRpc(OwnerClientId);
        }

        spellData = null;
    }

    public IEnumerator CastSpell(SpellData spell) {
        if (spell == null) yield break;

        if (IsOwner && spell.clearInHandBeforeAnim) {
            activeSpell.ClearInHandServerRpc(OwnerClientId);
        }

        spellData = spell;
    }

    private void OnSpellCasted(bool _) {
        if (spellData == null) return;
        if (IsOwner && !spellData.clearInHandBeforeAnim) {
            activeSpell.ClearInHandServerRpc(OwnerClientId);
        }

        StartCoroutine(spawnStrategy.Spawn(this, spellData));
    }

    private void OnBurst(bool _) {
        if (spellData == null) return;
        SpawnBurst(spellData.id);
    }

    public void SpawnProjectile(SpellData spell, Vector3 pos, Quaternion rot) {
        SpawnMainServerRpc(spell.id, pos, rot);
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

        var casterId = serverRpcParams.Receive.SenderClientId;
        netObj.SpawnWithOwnership(casterId);
        StartCoroutine(DespawnAndDestroyServer(netObj, spell.lifeTime));
        var caster = NetworkManager.Singleton.ConnectedClients[casterId].PlayerObject.GetComponent<NetworkStatSystem>();
        SpawnMainClientRpc(netObj.NetworkObjectId, spellId, caster.Stats.GetFinal(StatType.SpellDamage));
    }

    [ClientRpc]
    private void SpawnMainClientRpc(ulong objectId, int spellId, float damageMultiplier) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;

        var spell = SpellDatabase.Instance != null ? SpellDatabase.Instance.GetSpell(spellId) : null;

        if (netObj.TryGetComponent<SpellProjectile>(out var projectile) && spell != null) {
            projectile.Initialize(spell, damageMultiplier);
        }

        netObj.gameObject.SetActive(true);
        PlayParticleSystem(netObj.gameObject);
    }

    private void SpawnBurst(int spellId) {
        var spell = SpellDatabase.Instance != null ? SpellDatabase.Instance.GetSpell(spellId) : null;
        var burst = spell?.spellBurstPrefab;
        if (burst == null) return;
        GameObject obj = Instantiate(burst, spellCastPoint);
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

    private void PlayParticleSystem(GameObject obj) {
        if (obj == null) return;
        ParticleSystem[] psArray = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in psArray) {
            ps.Play();
        }
    }
}