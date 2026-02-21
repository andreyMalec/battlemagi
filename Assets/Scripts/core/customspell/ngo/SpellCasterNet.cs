using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpellCasterNet : NetworkBehaviour {
    private GameObject _spellPrefab;

    private void Awake() {
        _spellPrefab = GetComponent<SpellCaster>().spellPrefab;
    }

    public void RequestCast(SpellDefinition spell) {
        var spellJson = SpellJsonSerializer.ToJson(spell);
        RequestCastServerRpc(NetworkObjectId, spellJson);
    }

    [ServerRpc]
    private void RequestCastServerRpc(ulong casterNetObj, FixedString4096Bytes spellJson) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObj, out var netObj)) return;
        Debug.Log($"[NetworkSpellSystemEvent] OnCastServerRpc: {netObj.name}");
        var spell = SpellJsonSerializer.FromJson<SpellDefinition>(spellJson.Value);
        var caster = netObj.GetComponent<SpellCaster>();

        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = caster.Origin,
            rotation = Quaternion.LookRotation(caster.Direction, Vector3.up),
            forward = caster.Direction,
            caster = caster
        };
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
    }

    private void ServerSpawnMain(SpawnContext context) {
        var main = Instantiate(_spellPrefab, context.position, context.rotation);
        var networkObject = main.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(context.caster.OwnerId);
        var id = networkObject.NetworkObjectId;
        var casterNetObj = context.caster.GetComponent<NetworkObject>();
        OnCastClientRpc(id, casterNetObj.NetworkObjectId, context.forward, SpellJsonSerializer.ToJson(context.spell));
    }

    [ClientRpc]
    private void OnCastClientRpc(
        ulong spellNetObjectId,
        ulong casterNetObjectId,
        Vector3 forward,
        FixedString4096Bytes spellJson
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellNetObjectId, out var main)) return;
        Debug.Log(
            $"[NetworkSpellSystemEvent] OnCastClientRpc: netObjectId={spellNetObjectId}, caster={casterNetObj.gameObject.name}");
        var spell = SpellJsonSerializer.FromJson<SpellDefinition>(spellJson.Value);
        var caster = casterNetObj.GetComponent<SpellCaster>();

        var context = new SpawnContext {
            main = main.gameObject,
            spell = spell,
            forward = forward,
            caster = caster
        };
        caster.SpellSystem.CastSpell(context);
    }
}