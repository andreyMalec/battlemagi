using System;
using System.Collections;
using UnityEngine;

public interface ISpellSpawn {
    IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn);

    public static int InstanceCount(SpawnContext context) {
        var instanceMulti = 1f; //context.caster.statSystem.Stats.GetFinal(StatType.ProjectileCount);
        var instanceCount = (int)Math.Floor(context.spawn.instanceCount * instanceMulti);
        if (context.spawn.instanceLimit > 0 && instanceCount > context.spawn.instanceLimit)
            instanceCount = context.spawn.instanceLimit;
        return instanceCount;
    }

    public static Quaternion RotationFromNormal(Vector3 forwardHint, Vector3 normal) {
        var forwardOnPlane = Vector3.ProjectOnPlane(forwardHint, normal);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(Vector3.forward, normal);
        return Quaternion.LookRotation(forwardOnPlane.normalized, normal);
    }

    public static SpawnContext FromHit(SpawnContext context, RaycastHit hit, Vector3 direction) {
        return context with {
            position = hit.point + hit.normal * 0.15f,
            rotation = RotationFromNormal(direction, hit.normal),
            forward = Vector3.Reflect(direction, hit.normal.normalized)
        };
    }

    public static SpawnContext GroundPos(
        SpawnContext context,
        Vector3 direction,
        out RaycastHit hitInfo,
        Vector3 secondDirection = default
    ) {
        var maxDistance = context.spawn.raycastMaxDistance;
        var origin = context.position;
        var mask = context.spell.defaultRaycast;

        if (Physics.Raycast(origin, direction, out var hit, maxDistance, mask)) {
            if (secondDirection != Vector3.zero) {
                origin = hit.point + hit.normal * 0.15f;
                if (Physics.Raycast(origin, Vector3.down, out var hit2, maxDistance, context.spell.defaultRaycast)) {
                    hitInfo = hit2;
                    return FromHit(context, hit2, direction);
                }
            }

            hitInfo = hit;
            return FromHit(context, hit, direction);
        }

        if (Physics.Raycast(origin + direction * maxDistance, Vector3.down, out hit, maxDistance, mask)) {
            hitInfo = hit;
            return FromHit(context, hit, direction);
        }

        if (Physics.Raycast(origin + Vector3.up, Vector3.down, out hit, maxDistance, mask)) {
            hitInfo = hit;
            return FromHit(context, hit, direction);
        }

        hitInfo = new RaycastHit();
        return context with {
            position = new Vector3(1000, 0, 0),
            rotation = Quaternion.identity,
            forward = Vector3.zero
        };
    }
}