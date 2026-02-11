using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGroundPointSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;
        var onFirst = ISpellSpawn.GroundPos(context, context.forward, out _);
        for (int i = count - 1; i >= 0; i--) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(onFirst);
                    break;
                case DelayOrigin.Continuous:
                    var ctx = ISpellSpawn.GroundPos(context with {
                        position = context.caster.Origin,
                        rotation = Quaternion.LookRotation(context.caster.Direction),
                        forward = context.caster.Direction,
                    }, context.caster.Direction, out _);
                    spawn(ctx);
                    break;
            }

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        yield return ISpellSpawn.GroundPos(context, context.forward, out _);
    }
}