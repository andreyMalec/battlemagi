using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpellSpawn {
    private const float RayLength = 50f;

    IEnumerator Request(SpawnContext context, Action<SpawnContext> spawn);

    IEnumerable<SpawnContext> ShapeCenter(SpawnContext context);

    public static int InstanceCount(SpawnContext context) {
        var instanceMulti = 1f;
        if (!context.spell.spawn.disableInstanceMultiplier) {
            instanceMulti = context.caster.GetComponent<Stats>()?.GetFinal(StatType.ProjectileCount) ?? 1f;
        }

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
        var forward = Vector3.ProjectOnPlane(direction, hit.normal);
        if (forward.sqrMagnitude < 0.0001f)
            forward = direction;

        forward.Normalize();

        return context with {
            position = hit.point + hit.normal * 0.1f,
            rotation = RotationFromNormal(forward, hit.normal),
            forward = forward
        };
    }

    public static SpawnContext GroundPos(
        SpawnContext context,
        Vector3 direction,
        out RaycastHit hitInfo,
        Vector3 secondDirection = default
    ) {
        if (context.branch && context.spawn.spawnMode != SpawnMode.DirectDown) {
            hitInfo = new RaycastHit {
                point = context.position,
                normal = context.forward
            };
            return context;
        }

        var maxDistance = context.spawn.raycastMaxDistance;
        var origin = context.position;
        var mask = context.spell.defaultRaycast;

        if (Physics.Raycast(origin - direction * 0.1f, direction, out var hit, maxDistance, mask)) {
            if (secondDirection != Vector3.zero) {
                origin = hit.point + hit.normal * 0.1f;
                if (Physics.Raycast(origin, Vector3.down, out var hit2, maxDistance, context.spell.defaultRaycast)) {
                    hitInfo = hit2;
                    return FromHit(context, hit2, direction);
                }
            }

            hitInfo = hit;
            return FromHit(context, hit, direction);
        }

        if (Physics.Raycast(origin + direction * maxDistance, Vector3.down, out hit, RayLength, mask)) {
            hitInfo = hit;
            return FromHit(context, hit, direction);
        }

        if (Physics.Raycast(origin + Vector3.up, Vector3.down, out hit, RayLength, mask)) {
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

    public static ISpellSpawn GetMode(SpawnMode mode) {
        return mode switch {
            SpawnMode.Direct => new NewDirectSpawn(),
            SpawnMode.DirectDown => new NewDirectDownSpawn(),
            SpawnMode.DirectDownForward => new DirectDownForwardSpawn(),
            SpawnMode.Arc => new NewArcSpawn(),
            SpawnMode.GroundPoint => new NewGroundPointSpawn(),
            SpawnMode.GroundPointArc => new NewGroundPointArcSpawn(),
            SpawnMode.GroundPointArcDown => new NewGroundPointArcDownSpawn(),
            SpawnMode.GroundPointForward => new NewGroundPointForwardSpawn(),
            SpawnMode.GroundPointCircleUp => new GroundPointCircleUpSpawn(),
            SpawnMode.GroundPointDiskUp => new GroundPointDiskUpSpawn(),
            SpawnMode.Cone => new ConeSpawn(),
            SpawnMode.RayCast => new RayCastSpawn(),
            _ => null
        };
    }
}