using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectDownForwardSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var step = context.spawn.forwardStep;

        Physics.Raycast(context.position, Vector3.down, out var hit, context.spawn.raycastMaxDistance,
            context.spell.defaultRaycast, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) {
            var forward = RotationFromNormal(context.forward, hit.normal);
            spawn(context with {
                position = hit.point + forward * (step * i + 1),
                rotation = Quaternion.LookRotation(forward, Vector3.up),
                forward = forward
            });

            if (delay > 0f && i < count - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        yield return context;
    }

    private static Vector3 RotationFromNormal(Vector3 forwardHint, Vector3 normal) {
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(forwardHint, normal);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(Vector3.forward, normal);
        return forwardOnPlane;
    }
}