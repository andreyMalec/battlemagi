using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var origin = context.DelayOrigin;
        var delay = context.spawn.multiInstanceDelay;

        var radius = context.spawn.coneRadius;
        var height = context.spawn.coneHeight;

        for (int i = 1; i < count + 1; i++) {
            var ctx = origin switch {
                DelayOrigin.First => context,
                DelayOrigin.Continuous => context with {
                    position = context.caster.spawnPos.position,
                    rotation = context.caster.spawnPos.rotation,
                    forward = context.caster.spawnPos.forward,
                },
                _ => context
            };

            var denom = Mathf.Max(1, count);
            var t = Mathf.Clamp01((float)i / denom);
            var dist = height * t;

            var r = radius * t;
            var offset = RandomInsideCircle(r);

            var local = new Vector3(offset.x, offset.y, dist);
            var worldDir = (ctx.rotation * local).normalized;

            var pos = ctx.position + worldDir * local.magnitude;
            var rot = Quaternion.LookRotation(worldDir, Vector3.up);

            spawn(ctx with {
                position = pos,
                rotation = rot,
                forward = worldDir
            }, i);

            if (delay > 0f && i < count)
                yield return new WaitForSeconds(delay);
        }
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