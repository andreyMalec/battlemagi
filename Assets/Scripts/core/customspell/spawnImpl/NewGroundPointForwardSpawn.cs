using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGroundPointForwardSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var ground = ISpellSpawn.GroundPos(context, context.forward, out var hit);
        var step = context.spawn.forwardStep;

        for (int i = 0; i < count; i++) {
            var forward = RotationFromNormal(context.forward, hit.normal);
            spawn(ground with {
                position = ground.position + forward * (step * i),
                forward = forward
            });

            if (delay > 0f && i < count - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        yield return ISpellSpawn.GroundPos(context, context.forward, out _);
    }

    private static Vector3 RotationFromNormal(Vector3 forwardHint, Vector3 normal) {
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(forwardHint, normal);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(Vector3.forward, normal);
        return forwardOnPlane;
    }
}