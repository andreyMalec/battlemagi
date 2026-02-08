using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewArcSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var origin = context.DelayOrigin;
        var delay = context.spawn.multiInstanceDelay;
        var angleStep = context.spawn.arcAngleStep;
        float startAngle = -((count - 1) * angleStep) / 2f;

        for (int i = count - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            var ctx = origin switch {
                DelayOrigin.First => context,
                DelayOrigin.Continuous => context with {
                    position = context.caster.spawnPos.position,
                    rotation = context.caster.spawnPos.rotation,
                    forward = context.caster.spawnPos.forward,
                },
                _ => context
            };
            Quaternion rotation = ctx.rotation * Quaternion.Euler(0f, angle, 0f);
            spawn(ctx with {
                rotation = rotation,
                forward = rotation * Vector3.forward
            }, (int)angle);

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        yield return context;
    }
}