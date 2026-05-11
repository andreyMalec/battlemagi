using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);
        var delay = context.spawn.multiInstanceDelay;

        for (int i = 0; i < count; i++) {
            var ctx = context;
            if (ctx.DelayOrigin == DelayOrigin.Continuous) {
                ctx = ctx with {
                    position = ctx.caster.Origin,
                    rotation = Quaternion.LookRotation(ctx.caster.Direction),
                    forward = ctx.caster.Direction,
                };
            }

            ctx = ApplyDirectionToTarget(ctx);
            spawn(ctx);

            if (delay > 0f && i < count - 1)
                yield return new WaitForSeconds(delay);
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