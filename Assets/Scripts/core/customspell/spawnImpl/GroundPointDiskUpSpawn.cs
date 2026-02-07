using System;
using System.Collections;
using UnityEngine;

public class GroundPointDiskUpSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;

        var radius = context.spawn.circleRadius;
        var height = context.spawn.circleHeight;

        var ctx = ISpellSpawn.GroundPos(context, context.forward, out _, Vector3.down);

        for (int i = 0; i < count; i++) {
            var offset = RandomInsideCircle(radius);
            var localOffset = new Vector3(offset.x, height, offset.y);
            var pos = ctx.position + ctx.rotation * localOffset;

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

    private Vector2 RandomInsideCircle(float r) {
        if (r <= 0f) return Vector2.zero;
        var v = UnityEngine.Random.insideUnitCircle;
        var mag = Mathf.Sqrt(UnityEngine.Random.value) * r;
        if (v.sqrMagnitude > 0f) v.Normalize();
        return v * mag;
    }
}

