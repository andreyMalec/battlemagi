using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundPointDiskUpSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
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
            var rot = context.spell.coreType == CoreType.Projectile
                ? Quaternion.LookRotation(tiltedDown, Vector3.up)
                : Quaternion.identity;

            spawn(ctx with {
                position = pos,
                rotation = rot,
                forward = tiltedDown,
                forceFirstOrigin = true,
            });

            if (delay > 0f && i < count - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        var ground = ISpellSpawn.GroundPos(context, context.forward, out _, Vector3.down);
        yield return ground;

        var height = context.spawn.circleHeight;
        yield return ground with {
            position = ground.position + ground.rotation * new Vector3(0f, height, 0f)
        };
    }

    private Vector2 RandomInsideCircle(float r) {
        if (r <= 0f) return Vector2.zero;
        var v = UnityEngine.Random.insideUnitCircle;
        var mag = Mathf.Sqrt(UnityEngine.Random.value) * r;
        if (v.sqrMagnitude > 0f) v.Normalize();
        return v * mag;
    }
}