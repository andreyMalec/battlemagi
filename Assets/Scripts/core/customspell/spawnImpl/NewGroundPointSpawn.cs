using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGroundPointSpawn : ISpellSpawn, IDelayOriginRespect {
    public IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn) {
        var count = ISpellSpawn.InstanceCount(context);

        var delay = context.spawn.multiInstanceDelay;
        var origin = context.DelayOrigin;

        Debug.Log($"NewGroundPointSpawn: 0 target = {context.target?.Position}");
        var firstBase = BaseContext(context, origin, isFirst: true);
        Debug.Log($"NewGroundPointSpawn: 1 firstBase = {firstBase.position}, forward = {firstBase.forward}");
        var onFirst = ISpellSpawn.GroundPos(firstBase, firstBase.forward, out _);
        Debug.Log($"NewGroundPointSpawn: 2 firstBase = {onFirst.position}, forward = {onFirst.forward}");

        for (int i = count - 1; i >= 0; i--) {
            switch (origin) {
                case DelayOrigin.First:
                    spawn(onFirst);
                    break;
                case DelayOrigin.Continuous:
                    var currentBase = BaseContext(context, origin, isFirst: false);
                    var ctx = ISpellSpawn.GroundPos(currentBase, currentBase.forward, out _);
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
        yield return ISpellSpawn.GroundPos(baseCtx, baseCtx.forward, out _);
    }
}