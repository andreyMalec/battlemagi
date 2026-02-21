using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpellCasterNet : NetworkBehaviour {
    private GameObject _spellPrefab;

    private GameObject SpellPrefab {
        get {
            if (_spellPrefab != null) return _spellPrefab;
            var caster = GetComponent<SpellCaster>();
            if (caster == null) {
                caster = GetComponentInChildren<SpellCaster>();
            }

            _spellPrefab = caster.spellPrefab;

            return _spellPrefab;
        }
    }

    public void RequestCast(SpellDefinition spell) {
        var spellJson = SpellJsonSerializer.ToJson(spell);
        RequestCastServerRpc(NetworkObjectId, spellJson);
    }

    public void RequestSpawn(SpawnContext context) {
        var spellJson = SpellJsonSerializer.ToJson(context.spell);
        RequestSpawnServerRpc(NetworkObjectId, spellJson, context.position, context.forward, context.rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnServerRpc(
        ulong casterNetObjectId,
        FixedString4096Bytes spellJson,
        Vector3 position, Vector3 forward, Quaternion rotation
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        Debug.Log(
            $"[NetworkSpellSystemEvent] RequestSpawnServerRpc: {casterNetObj.name}, position={position}, forward={forward}");
        var spell = SpellJsonSerializer.FromJson<SpellDefinition>(spellJson.Value);
        var caster = casterNetObj.GetComponent<SpellCaster>();

        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = position,
            rotation = rotation,
            forward = forward,
            caster = caster,
            forceFirstOrigin = true
        };
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
    }

    [ServerRpc]
    private void RequestCastServerRpc(ulong casterNetObjectId, FixedString4096Bytes spellJson) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        Debug.Log($"[NetworkSpellSystemEvent] RequestCastServerRpc: {casterNetObj.name}");
        var spell = SpellJsonSerializer.FromJson<SpellDefinition>(spellJson.Value);
        var caster = casterNetObj.GetComponent<SpellCaster>();

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
        var main = Instantiate(SpellPrefab, context.position, context.rotation);
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