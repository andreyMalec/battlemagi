using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewDirectDownSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);
        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;
        var onFirst = context with {
            position = context.caster.transform.position,
            rotation = context.caster.transform.rotation,
            forward = context.caster.transform.forward,
        };
        for (int i = 0; i < count; i++) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(onFirst);
                    break;
                case DelayOrigin.Continuous:
                    spawn(context with {
                        position = context.caster.transform.position,
                        rotation = context.caster.transform.rotation,
                        forward = context.caster.transform.forward,
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