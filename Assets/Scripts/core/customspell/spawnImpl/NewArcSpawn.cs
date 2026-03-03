using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewArcSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var origin = context.DelayOrigin;
        var delay = context.spawn.multiInstanceDelay;
        var angleStep = context.spawn.arcAngleStep;
        float startAngle = -((count - 1) * angleStep) / 2f;

        var first = ApplyBaseByOrigin(context, origin, isFirst: true);
        first = ApplyDirectionToTarget(first);

        for (int i = count - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            var ctx = origin == DelayOrigin.First
                ? first
                : ApplyDirectionToTarget(ApplyBaseByOrigin(context, origin, isFirst: false));

            Quaternion rotation = ctx.rotation * Quaternion.Euler(0f, angle, 0f);
            spawn(ctx with {
                rotation = rotation,
                forward = rotation * Vector3.forward
            });

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    private static SpawnContext ApplyBaseByOrigin(SpawnContext context, DelayOrigin origin, bool isFirst) {

        if (origin == DelayOrigin.Continuous && !isFirst) {
            return context with {
                position = context.caster.Origin,
                rotation = Quaternion.LookRotation(context.caster.Direction),
                forward = context.caster.Direction,
            };
        }

        return context;
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