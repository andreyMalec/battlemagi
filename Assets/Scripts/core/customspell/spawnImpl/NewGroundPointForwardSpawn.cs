using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGroundPointForwardSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var baseCtx = ApplyDirectionToTarget(context);

        var ground = ISpellSpawn.GroundPos(baseCtx, baseCtx.forward, out var hit);
        var step = baseCtx.spawn.forwardStep;

        for (int i = 0; i < count; i++) {
            var forward = RotationFromNormal(baseCtx.forward, hit.normal);
            spawn(ground with {
                position = ground.position + forward * (step * i),
                forward = forward
            });

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
        var baseCtx = context.target != null ? context with { position = context.target.Position } : context;
        yield return ISpellSpawn.GroundPos(baseCtx, baseCtx.forward, out _);
    }

    private static Vector3 RotationFromNormal(Vector3 forwardHint, Vector3 normal) {
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(forwardHint, normal);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(Vector3.forward, normal);
        return forwardOnPlane;
    }
}