using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewDirectSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);
        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;
        for (int i = 0; i < count; i++) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(context);
                    break;
                case DelayOrigin.Continuous:
                    spawn(context with {
                        position = context.caster.Origin,
                        rotation = Quaternion.LookRotation(context.caster.Direction),
                        forward = context.caster.Direction,
                    });
                    break;
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