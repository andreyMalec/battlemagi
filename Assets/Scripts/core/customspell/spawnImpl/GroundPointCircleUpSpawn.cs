using System;
using System.Collections;
using UnityEngine;

public class GroundPointCircleUpSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var origin = context.DelayOrigin;
        var delay = context.spawn.multiInstanceDelay;

        var radius = context.spawn.circleRadius;
        var height = context.spawn.circleHeight;

        var ctx = ISpellSpawn.GroundPos(context, context.forward, out _, Vector3.down);

        for (int i = 0; i < count; i++) {

            var angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var localOffset = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            var worldOffset = ctx.rotation * localOffset;

            var pos = ctx.position + worldOffset;

            var down = -(ctx.rotation * Vector3.up);
            var tilt = UnityEngine.Random.insideUnitCircle * 0.2f;
            var tiltedDown = (down + ctx.rotation * new Vector3(tilt.x, 0f, tilt.y)).normalized;

            var rot = Quaternion.LookRotation(tiltedDown, Vector3.up);

            spawn(ctx with {
                position = pos,
                rotation = rot,
                forward = tiltedDown,
                forceFirstOrigin = true,
            }, i);

            if (delay > 0f && i < count - 1)
                yield return new WaitForSeconds(delay);
        }
    }
}
