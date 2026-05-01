using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var origin = context.DelayOrigin;
        var delay = context.spawn.multiInstanceDelay;

        var radius = context.spawn.coneRadius;
        var height = context.spawn.coneHeight;

        var first = ApplyBaseByOrigin(context, origin, isFirst: true);
        first = ApplyDirectionToTarget(first);

        for (int i = 1; i < count + 1; i++) {
            var ctx = origin == DelayOrigin.First
                ? first
                : ApplyDirectionToTarget(ApplyBaseByOrigin(context, origin, isFirst: false));

            var denom = Mathf.Max(1, count);
            var t = Mathf.Clamp01((float)i / denom);
            var dist = height * t;

            var r = radius * t;
            var offset = RandomInsideCircle(r);

            var local = new Vector3(offset.x, offset.y, dist);
            var worldDir = (ctx.rotation * local).normalized;

            var pos = ctx.position + worldDir * local.magnitude;
            var rot2 = Quaternion.LookRotation(worldDir, Vector3.up);

            spawn(ctx with {
                position = pos,
                rotation = rot2,
                forward = worldDir
            });

            if (delay > 0f && i < count)
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

    private Vector2 RandomInsideCircle(float r) {
        if (r <= 0f) return Vector2.zero;
        var v = UnityEngine.Random.insideUnitCircle;
        v.Normalize();
        var mag = Mathf.Sqrt(UnityEngine.Random.value) * r;
        return v * mag;
    }
}