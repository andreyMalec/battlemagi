using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewDirectDownSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);
        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;

        var downWithDirection = Vector3.down + context.forward * 0.01f;
        var first = ISpellSpawn.GroundPos(context, downWithDirection, out _);
        // first = ApplyDirectionToTarget(first);

        for (int i = 0; i < count; i++) {
            if (origin == DelayOrigin.First) {
                spawn(first);
            } else {
                context.position = context.caster.transform.position;
                var current = ISpellSpawn.GroundPos(context, downWithDirection, out _);
                // current = ApplyDirectionToTarget(current);
                spawn(current);
            }

            if (delay > 0f && i < count - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private static SpawnContext ApplyDirectionToTarget(SpawnContext context) {
        if (context.target == null)
            return context;

        var dir = context.target.Position - context.position;
        if (dir.sqrMagnitude <= 0f)
            return context;

        var forward = dir.normalized;
        return context with {
            rotation = Quaternion.LookRotation(forward, Vector3.up),
            forward = forward,
        };
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        yield return context;
    }
}