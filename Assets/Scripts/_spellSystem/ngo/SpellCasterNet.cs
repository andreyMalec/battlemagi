using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpellCasterNet : NetworkBehaviour {
    public Coroutine CastCoroutine;

    private readonly Dictionary<FixedString64Bytes, List<FixedString4096Bytes>> _pendingChunks = new();

    public void RequestCast(SpawnContext context) {
        var json = SpellJsonSerializer.ToJson(context.spell);
        var id = SpellNetworkCodec.ComputeId(json);
        SpellNetworkCache.Put(id, json);
        SendSpellToServer(id, json);
        RequestCastServerRpc(NetworkObjectId, id, context.target?.ObjectId ?? ulong.MaxValue,
            context.spellDamageMultiplier);
    }

    public void RequestSpawn(SpawnContext context) {
        var json = SpellJsonSerializer.ToJson(context.spell);
        var id = SpellNetworkCodec.ComputeId(json);
        SpellNetworkCache.Put(id, json);
        SendSpellToServer(id, json);
        RequestSpawnServerRpc(NetworkObjectId, id, context.position, context.forward, context.rotation,
            context.spellDamageMultiplier);
    }

    private void SendSpellToServer(FixedString64Bytes id, string json) {
        var chunks = SpellNetworkCodec.Chunk(json);
        for (int i = 0; i < chunks.Count; i++) {
            UploadSpellChunkServerRpc(id, chunks[i], i, chunks.Count);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UploadSpellChunkServerRpc(
        FixedString64Bytes id,
        FixedString4096Bytes jsonChunk,
        int chunkIndex,
        int chunkCount
    ) {
        if (!_pendingChunks.TryGetValue(id, out var list)) {
            list = new List<FixedString4096Bytes>(chunkCount);
            for (int i = 0; i < chunkCount; i++) list.Add(default);
            _pendingChunks[id] = list;
        }

        if (chunkIndex >= 0 && chunkIndex < list.Count)
            list[chunkIndex] = jsonChunk;

        bool complete = true;
        for (int i = 0; i < list.Count; i++) {
            if (list[i].Length == 0) {
                complete = false;
                break;
            }
        }

        if (!complete) return;

        var assembled = SpellNetworkCodec.Assemble(list);
        SpellNetworkCache.Put(id, assembled);
        _pendingChunks.Remove(id);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnServerRpc(
        ulong casterNetObjectId,
        FixedString64Bytes spellId,
        Vector3 position, Vector3 forward, Quaternion rotation,
        float damageMultiplier
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        if (!SpellNetworkCache.TryGet(spellId, out var json))
            return;

        Debug.Log(
            $"[NetworkSpellSystemEvent] RequestSpawnServerRpc: {casterNetObj.name}, position={position}, forward={forward}, damageMultiplier={damageMultiplier}");
        var spell = SpellJsonSerializer.FromJson<SpellDefinition>(json);
        var caster = casterNetObj.GetComponentInChildren<SpellCaster>();

        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = position,
            rotation = rotation,
            forward = forward,
            caster = caster,
            forceFirstOrigin = true,
            spellDamageMultiplier = damageMultiplier
        };
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCastServerRpc(
        ulong casterNetObjectId,
        FixedString64Bytes spellId,
        ulong targetNetObjectId = ulong.MaxValue,
        float damageMultiplier = 1f
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        if (!SpellNetworkCache.TryGet(spellId, out var json))
            return;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId, out var targetNetObj);
        ITarget target = null;
        if (targetNetObj != null && targetNetObj.IsSpawned) {
            target = targetNetObj.GetComponentInChildren<ITarget>();
        }

        Debug.Log(
            $"[NetworkSpellSystemEvent] RequestCastServerRpc: {casterNetObj.name}, target={target}, damageMultiplier={damageMultiplier}");
        var spell = SpellJsonSerializer.FromJson<SpellDefinition>(json);
        var caster = casterNetObj.GetComponentInChildren<SpellCaster>();

        var context = caster.CastContext(spell);
        context.target = target;
        context.spellDamageMultiplier = damageMultiplier;
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        CastCoroutine = StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
    }

    private void ServerSpawnMain(SpawnContext context) {
        var casterNetObj = context.caster.GetComponentInParent<NetworkObject>();
        if (!casterNetObj.IsSpawned) return;
        var prefab = SpellPrefab.Instance.GetPrefab(true);
        var main = Instantiate(prefab, context.position, context.rotation);
        var networkObject = main.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(context.caster.OwnerId);
        var id = networkObject.NetworkObjectId;

        var prefabId = context.spell.coreType switch {
            CoreType.Projectile => (int)context.spell.projectile.prefabId,
            CoreType.Zone => (int)context.spell.zone.prefabId,
            CoreType.Beam => (int)context.spell.beam.prefabId,
            CoreType.Summon => (int)context.spell.summon.prefabId,
            _ => -1
        };
        context.main = main;
        context.caster.SpellSystem.CastSpell(context);
        var excludeHost = new ClientRpcParams {
            Send = new ClientRpcSendParams
                { TargetClientIds = NetworkManager.ConnectedClients.Keys.Filter(it => it > 0).ToArray() }
        };
        if (prefabId > -1)
            OnCastClientRpc(id, casterNetObj.NetworkObjectId, (int)context.spell.coreType, prefabId,
                context.spell.scale, context.spell.lifetime, excludeHost);
    }

    [ClientRpc]
    private void OnCastClientRpc(
        ulong spellNetObjectId,
        ulong casterNetObjectId,
        int coreType,
        int prefabId,
        float scale,
        float lifetime,
        ClientRpcParams rpcParams = default
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellNetObjectId, out var main)) return;

        Debug.Log(
            $"[NetworkSpellSystemEvent] OnCastClientRpc: netObjectId={spellNetObjectId}, caster={casterNetObj.gameObject.name}");
        var caster = casterNetObj.GetComponentInChildren<SpellCaster>();

        caster.SpellSystem.ShowSpell(main.gameObject, (CoreType)coreType, prefabId);
        var instance = main.GetComponentInChildren<SpellInstance>();
        instance.Scale(scale, lifetime);
    }
}