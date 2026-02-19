// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;
//
// public class NgoSpellSpawner : NetworkBehaviour, SpellSpawner {
//     public void RequestSpawn(SpellDefinition spell) {
//         var context = new SpawnContext {
//             spell = spell,
//             spawn = spell.spawn,
//             position = Origin,
//             rotation = Quaternion.LookRotation(Direction, Vector3.up),
//             forward = Direction,
//             caster = this
//         };
//         var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
//         StartCoroutine(spellSpawn!.Request(context, Cast));
//     }
//     
//     
//     public void OnCast(SpellCaster caster, SpellDefinition spell) {
//         OnCastServerRpc(caster.OwnerId, SpellJsonSerializer.ToJson(spell));
//     }
//
//     [ServerRpc]
//     private void OnCastServerRpc(ulong ownerId, FixedString4096Bytes spellJson) {
//         if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out var client)) return;
//         var player = client.PlayerObject;
//         if (player == null) return;
//         Debug.Log($"[NetworkSpellSystemEvent] OnCastServerRpc: {ownerId}");
//         var spell = SpellJsonSerializer.FromJson<SpellDefinition>(spellJson.Value);
//         var caster = player.GetComponent<SpellCaster>();
//
//         var context = new SpawnContext {
//             spell = spell,
//             spawn = spell.spawn,
//             position = caster.Origin,
//             rotation = Quaternion.LookRotation(caster.Direction, Vector3.up),
//             forward = caster.Direction,
//             caster = caster
//         };
//         var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
//         StartCoroutine(spellSpawn!.Request(context, ServerSpawnMain));
//     }
//
//     private void ServerSpawnMain(SpawnContext context) {
//         var obj = Instantiate(_spellPrefab, context.position, context.rotation);
//         var networkObject = obj.GetComponent<NetworkObject>();
//         networkObject.SpawnWithOwnership(context.caster.OwnerId);
//         var id = networkObject.NetworkObjectId;
//         OnCastClientRpc(id, context.caster.OwnerId, context.forward, SpellJsonSerializer.ToJson(context.spell));
//     }
//
//     [ClientRpc]
//     private void OnCastClientRpc(ulong netObjectId, ulong ownerId, Vector3 forward, FixedString4096Bytes spellJson) {
//         if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out var client)) return;
//         var player = client.PlayerObject;
//         if (player == null) return;
//         Debug.Log($"[NetworkSpellSystemEvent] OnCastClientRpc: netObjectId={netObjectId}, ownerId={ownerId}");
//         var spell = SpellJsonSerializer.FromJson<SpellDefinition>(spellJson.Value);
//         var caster = player.GetComponent<SpellCaster>();
//         if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjectId, out var main)) return;
//
//         var context = new SpawnContext {
//             main = main.gameObject,
//             spell = spell,
//             forward = forward,
//             caster = caster
//         };
//         caster.Cast(context);
//     }
// }