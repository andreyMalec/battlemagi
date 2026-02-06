using System;
using System.Collections;
using UnityEngine;

public class OnFeetSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);
        var delay = context.spawn.multiInstanceDelay;
        var origin = context.spawn.delayOrigin;
        var onFirst = context with {
            position = context.caster.transform.position,
            rotation = context.caster.transform.rotation,
            forward = context.caster.transform.forward,
        };
        for (int i = 0; i < count; i++) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(onFirst, i);
                    break;
                case DelayOrigin.Continuous:
                    spawn(context with {
                        position = context.caster.transform.position,
                        rotation = context.caster.transform.rotation,
                        forward = context.caster.transform.forward,
                    }, i);
                    break;
            }

            if (delay > 0f && i < count - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}