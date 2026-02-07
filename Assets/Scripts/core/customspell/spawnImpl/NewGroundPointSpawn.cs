using System;
using System.Collections;
using UnityEngine;

public class NewGroundPointSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var origin = context.spawn.delayOrigin;
        var onFirst = ISpellSpawn.GroundPos(context, context.forward, out _);
        for (int i = count - 1; i >= 0; i--) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(onFirst, i);
                    break;
                case DelayOrigin.Continuous:
                    var ctx = ISpellSpawn.GroundPos(context with {
                        position = context.caster.spawnPos.position,
                        rotation = context.caster.spawnPos.rotation,
                        forward = context.caster.spawnPos.forward,
                    }, context.caster.spawnPos.forward, out _);
                    spawn(ctx, i);
                    break;
            }

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }
}