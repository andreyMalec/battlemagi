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

        for (int i = 0; i < count; i++) {
            if (origin == DelayOrigin.First) {
                if (first != null)
                    spawn(first);
            } else {
                context.position = context.caster.transform.position;
                var current = ISpellSpawn.GroundPos(context, downWithDirection, out _);
                if (current != null)
                    spawn(current);
            }

            if (delay > 0f && i < count - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        yield return context;
    }
}