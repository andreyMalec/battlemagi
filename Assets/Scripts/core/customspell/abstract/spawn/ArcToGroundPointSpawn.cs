// using System;
// using System.Collections;
// using UnityEngine;
//
// public class ArcToGroundPointSpawn : ISpellSpawn {
//     public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
//         var count = ISpellSpawn.InstanceCount(context);
//
//         var delay = context.spawn.multiInstanceDelay;
//         var angleStep = context.spawn.arcAngleStep;
//         float startAngle = -((count - 1) * angleStep) / 2f;
//
//         for (int i = count - 1; i >= 0; i--) {
//             float angle = startAngle + angleStep * i;
//
//             Quaternion rotation = context.rotation * Quaternion.Euler(0f, angle, 0f);
//             var (groundPos, groundRot) = GetGroundPose(context, rotation * Vector3.forward);
//             spawn(context with {
//                 position = groundPos,
//                 rotation = groundRot
//             }, (int)angle);
//
//             if (delay > 0f && i > 0)
//                 yield return new WaitForSeconds(delay);
//         }
//     }
//
//     private (Vector3 position, Quaternion rotation) GetGroundPose(SpawnContext context, Vector3 direction) {
//         int mask = 1 << context.spell.defaultRaycast;
//         var maxDistance = context.spawn.raycastMaxDistance;
//         var origin = context.position;
//
//         if (Physics.Raycast(origin, direction, out var hit, maxDistance, mask)) {
//             origin = hit.point + hit.normal * 0.3f;
//             if (Physics.Raycast(origin, Vector3.down, out var hit2, maxDistance, mask)) {
//                 return (hit2.point + hit2.normal * 0.3f, ISpellSpawn.RotationFromNormal(direction, hit2.normal));
//             }
//
//             return (hit.point + hit.normal * 0.3f, ISpellSpawn.RotationFromNormal(direction, hit.normal));
//         }
//
//         if (Physics.Raycast(origin + direction * maxDistance, Vector3.down, out hit, maxDistance, mask)) {
//             return (hit.point + hit.normal * 0.3f, ISpellSpawn.RotationFromNormal(direction, hit.normal));
//         }
//
//         if (Physics.Raycast(origin + Vector3.up, Vector3.down, out hit, maxDistance, mask)) {
//             return (hit.point + hit.normal * 0.3f, ISpellSpawn.RotationFromNormal(direction, hit.normal));
//         }
//
//         return (new Vector3(1000, 0, 0), Quaternion.identity);
//     }
// }