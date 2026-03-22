using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundPointDiskUpSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;

        var radius = context.spawn.circleRadius;
        var height = context.spawn.circleHeight;

        var firstBase = BaseContext(context, origin, isFirst: true);
        var onFirst = ISpellSpawn.GroundPos(firstBase, firstBase.forward, out _, Vector3.down);

        for (int i = 0; i < count; i++) {
            var baseCtx = origin == DelayOrigin.First
                ? onFirst
                : ISpellSpawn.GroundPos(BaseContext(context, origin, isFirst: false), context.forward, out _, Vector3.down);

            var offset = RandomInsideCircle(radius);
            var localOffset = new Vector3(offset.x, height, offset.y);
            var pos = baseCtx.position + baseCtx.rotation * localOffset;

            var down = -(baseCtx.rotation * Vector3.up);
            var tilt = UnityEngine.Random.insideUnitCircle * 0.2f;
            var tiltedDown = (down + baseCtx.rotation * new Vector3(tilt.x, 0f, tilt.y)).normalized;
            var rot = context.spell.coreType == CoreType.Projectile
                ? Quaternion.LookRotation(tiltedDown, Vector3.up)
                : Quaternion.identity;

            spawn(baseCtx with {
                position = pos,
                rotation = rot,
                forward = tiltedDown,
                forceFirstOrigin = true,
            });

            if (delay > 0f && i < count - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    private static SpawnContext BaseContext(SpawnContext context, DelayOrigin origin, bool isFirst) {
        if (context.target != null) {
            if (origin == DelayOrigin.First && !isFirst)
                return context;

            return context with {
                position = context.target.Position,
                forward = Vector3.down,
            };
        }

        if (origin == DelayOrigin.Continuous && !isFirst) {
            return context with {
                position = context.caster.Origin,
                rotation = Quaternion.LookRotation(context.caster.Direction),
                forward = context.caster.Direction,
            };
        }

        return context;
    }

    public IEnumerable<SpawnContext> ShapeCenter(SpawnContext context) {
        var baseCtx = context.target != null
            ? context with { position = context.target.Position }
            : context;
        var ground = ISpellSpawn.GroundPos(baseCtx, baseCtx.forward, out _, Vector3.down);
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