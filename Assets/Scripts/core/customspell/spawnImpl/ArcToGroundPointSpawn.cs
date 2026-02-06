using System;
using System.Collections;
using UnityEngine;

public class ArcToGroundPointSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var angleStep = context.spawn.arcAngleStep;
        float startAngle = -((count - 1) * angleStep) / 2f;

        for (int i = count - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            Quaternion rotation = context.rotation * Quaternion.Euler(0f, angle, 0f);
            var ctx = GetGroundPose(context, rotation * Vector3.forward);
            spawn(ctx, (int)angle);

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    private SpawnContext GetGroundPose(SpawnContext context, Vector3 direction) {
        var maxDistance = context.spawn.raycastMaxDistance;
        var origin = context.position;
        var mask = context.spell.defaultRaycast;

        if (Physics.Raycast(origin, direction, out var hit, maxDistance, mask)) {
            origin = hit.point + hit.normal * 0.3f;
            if (Physics.Raycast(origin, Vector3.down, out var hit2, maxDistance, context.spell.defaultRaycast)) {
                return context with {
                    position = hit2.point + hit2.normal * 0.3f,
                    rotation = ISpellSpawn.RotationFromNormal(direction, hit.normal),
                    forward = Vector3.Reflect(direction, hit.normal.normalized)
                };
            }

            return ISpellSpawn.FromHit(context, hit, direction);
        }

        if (Physics.Raycast(origin + direction * maxDistance, Vector3.down, out hit, maxDistance, mask)) {
            return ISpellSpawn.FromHit(context, hit, direction);
        }

        if (Physics.Raycast(origin + Vector3.up, Vector3.down, out hit, maxDistance, mask)) {
            return ISpellSpawn.FromHit(context, hit, direction);
        }

        return context with {
            position = new Vector3(1000, 0, 0),
            rotation = Quaternion.identity,
            forward = Vector3.zero
        };
    }
}