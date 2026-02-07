using System;
using System.Collections;
using UnityEngine;

public class NewGroundPointArcDownSpawn : ISpellSpawn {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var angleStep = context.spawn.arcAngleStep;
        float startAngle = -((count - 1) * angleStep) / 2f;

        for (int i = count - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            Quaternion rotation = context.rotation * Quaternion.Euler(0f, angle, 0f);
            var ctx = ISpellSpawn.GroundPos(context, rotation * Vector3.forward, out _, Vector3.down);
            spawn(ctx, (int)angle);

            if (delay > 0f && i > 0)
                yield return new WaitForSeconds(delay);
        }
    }
}