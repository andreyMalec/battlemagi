using System;
using System.Collections;
using UnityEngine;

public interface ISpellSpawn {
    IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn);

    public static int InstanceCount(SpawnContext context) {
        var instanceMulti = 1f;//context.caster.statSystem.Stats.GetFinal(StatType.ProjectileCount);
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
}