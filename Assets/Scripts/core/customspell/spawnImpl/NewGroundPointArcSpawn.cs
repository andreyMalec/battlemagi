using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGroundPointArcSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var angleStep = context.spawn.arcAngleStep;
        float startAngle = -((count - 1) * angleStep) / 2f;

        var baseCtx = ApplyDirectionToTarget(context);

        for (int i = count - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            Quaternion rotation = baseCtx.rotation * Quaternion.Euler(0f, angle, 0f);
            var ctx = ISpellSpawn.GroundPos(baseCtx, rotation * Vector3.forward, out _);
            spawn(ctx);

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    private static SpawnContext ApplyDirectionToTarget(SpawnContext context) {
        if (context.target == null)
            return context;

        var targetPos = ISpellSpawn.GroundPos(context with { position = context.target.Position }, Vector3.down, out _);
        if (targetPos == null)
            return context;
        var dir = targetPos.position - context.position;
        if (dir.sqrMagnitude <= 0f)
            return context;

        var forward = dir.normalized;
        return context with {
            rotation = Quaternion.LookRotation(forward, Vector3.up),
            forward = forward,
        };
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        var baseCtx = context.target != null ? context with { position = context.target.Position } : context;
        yield return ISpellSpawn.GroundPos(baseCtx, baseCtx.forward, out _);
    }
}