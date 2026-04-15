using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpellCasterNet : NetworkBehaviour {
    public Coroutine CastCoroutine;

    public void RequestCast(SpawnContext context) {
        RequestCastServerRpc(NetworkObjectId, context.spell.words, context.alternativeSpawn,
            context.target?.ObjectId ?? ulong.MaxValue,
            context.spellDamageMultiplier);
    }

    public void RequestSpawn(SpawnContext context) {
        RequestSpawnServerRpc(NetworkObjectId, context.spell.words, context.position, context.forward, context.rotation,
            context.spellDamageMultiplier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnServerRpc(
        ulong casterNetObjectId,
        string spellWords,
        Vector3 position, Vector3 forward, Quaternion rotation,
        float damageMultiplier
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;

        Debug.Log(
            $"[NetworkSpellSystemEvent] RequestSpawnServerRpc: {casterNetObj.name}, position={position}, forward={forward}, damageMultiplier={damageMultiplier}");
        var spell = DefaultSpells.Get(spellWords)?.spell ?? DefaultSpells.GetSubSpell(spellWords);
        var caster = casterNetObj.GetComponentInChildren<SpellCaster>();

        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = position,
            rotation = rotation,
            forward = forward,
            caster = caster,
            forceFirstOrigin = true,
            spellDamageMultiplier = damageMultiplier,
            branch = true
        };
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCastServerRpc(
        ulong casterNetObjectId,
        string spellWords,
        bool alternativeSpawn,
        ulong targetNetObjectId = ulong.MaxValue,
        float damageMultiplier = 1f
    ) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId, out var targetNetObj);
        ITarget target = null;
        if (targetNetObj != null && targetNetObj.IsSpawned) {
            target = targetNetObj.GetComponentInChildren<ITarget>();
        }

        Debug.Log(
            $"[NetworkSpellSystemEvent] RequestCastServerRpc: {casterNetObj.name}, target={target}, damageMultiplier={damageMultiplier}");
        var spell = DefaultSpells.Get(spellWords)?.spell ?? DefaultSpells.GetSubSpell(spellWords);
        var caster = casterNetObj.GetComponentInChildren<SpellCaster>();

        var context = caster.CastContext(spell);
        context.target = target;
        context.spellDamageMultiplier = damageMultiplier;
        var spellSpawn = ISpellSpawn.GetMode(alternativeSpawn && spell.spawn.useAlternativeSpawnMode
            ? spell.spawn.alternativeSpawnMode
            : spell.spawn.spawnMode);
        CastCoroutine = StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
    }

    private void ServerSpawnMain(SpawnContext context) {
        var casterNetObj = context.caster.GetComponentInParent<NetworkObject>();
        if (!casterNetObj.IsSpawned) return;
        EnsureCasterInitialized(casterNetObj.gameObject, context.caster);
        var prefab = SpellPrefab.Instance.GetPrefab(true);
        var main = Instantiate(prefab, context.position, context.rotation);
        var networkObject = main.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(context.caster.OwnerId);
        var id = networkObject.NetworkObjectId;
        context.caster.HandleSpellLimit(context.spell, main);

        var prefabId = context.spell.coreType switch {
            CoreType.Projectile => (int)context.spell.projectile.prefabId,
            CoreType.Zone => (int)context.spell.zone.prefabId,
            CoreType.Beam => (int)context.spell.beam.prefabId,
            CoreType.Summon => (int)context.spell.summon.prefabId,
            _ => -1
        };
        var impassableForEnemies = context.spell.coreType is CoreType.Zone && context.spell.zone.impassableForEnemies;
        context.main = main;
        context.caster.SpellSystem.CastSpell(context);
        var excludeHost = new ClientRpcParams {
            Send = new ClientRpcSendParams
                { TargetClientIds = NetworkManager.ConnectedClients.Keys.Filter(it => it > 0).ToArray() }
        };
        if (prefabId > -1)
            OnCastClientRpc(id, casterNetObj.NetworkObjectId, (int)context.spell.coreType, prefabId,
                context.spell.scale, context.spell.lifetime, impassableForEnemies, excludeHost);
    }

    [ClientRpc]
    private void OnCastClientRpc(
        ulong spellNetObjectId,
        ulong casterNetObjectId,
        int coreType,
        int prefabId,
        float scale,
        float lifetime,
        bool impassableForEnemies,
        ClientRpcParams rpcParams = default
    ) {
        _ = rpcParams;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterNetObjectId, out var casterNetObj))
            return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellNetObjectId, out var main)) return;

        Debug.Log(
            $"[NetworkSpellSystemEvent] OnCastClientRpc: netObjectId={spellNetObjectId}, caster={casterNetObj.gameObject.name}");
        var caster = casterNetObj.GetComponentInChildren<SpellCaster>();
        EnsureCasterInitialized(casterNetObj.gameObject, caster);

        caster.SpellSystem.ShowSpell(main.gameObject, (CoreType)coreType, prefabId);
        var instance = main.GetComponentInChildren<SpellInstance>();
        instance.Scale(scale, lifetime);
        if (impassableForEnemies)
            ZoneEnemyColliderBlocker.Attach(main.gameObject, main.OwnerClientId, scale,
                instance.GetComponent<SpellView>());
    }

    private static void EnsureCasterInitialized(GameObject root, SpellCaster caster) {
        if (caster.SpellSystem != null)
            return;

        foreach (var behaviour in root.GetComponents<MonoBehaviour>()) {
            if (behaviour is not SpellBootstrap bootstrap)
                continue;

            bootstrap.Init(caster);
            return;
        }
    }
}