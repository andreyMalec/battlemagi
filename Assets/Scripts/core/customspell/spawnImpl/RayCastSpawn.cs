using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;

        var firstBase = BaseContext(context, origin, isFirst: true);
        var onFirst = RayCast(firstBase, firstBase.forward);

        for (int i = count - 1; i >= 0; i--) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(onFirst);
                    break;
                case DelayOrigin.Continuous:
                    var currentBase = BaseContext(context, origin, isFirst: false);
                    var ctx = RayCast(currentBase, currentBase.forward);
                    spawn(ctx);
                    break;
            }

            if (delay > 0f && i > 0)
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
        yield return RayCast(baseCtx, baseCtx.forward);
    }

    private static SpawnContext RayCast(
        SpawnContext context,
        Vector3 direction
    ) {
        if (context.branch) {
            return context;
        }

        var maxDistance = context.spawn.raycastMaxDistance;
        var origin = context.position;
        var mask = context.spell.defaultRaycast;

        if (Physics.Raycast(origin - direction * 0.1f, direction, out var hit, maxDistance, mask)) {
            return ISpellSpawn.FromHit(context, hit, direction);
        }

        return context with {
            position = origin + direction * maxDistance,
            rotation = Quaternion.identity,
            forward = Vector3.zero
        };
    }
}