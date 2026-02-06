using System;
using System.Collections;
using UnityEngine;

public interface ISpellSpawn {
    IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn);

    public static int InstanceCount(SpawnContext context) {
        var instanceMulti = context.caster.statSystem.Stats.GetFinal(StatType.ProjectileCount);
        var instanceCount = (int)Math.Floor(context.data.instanceCount * instanceMulti);
        if (context.data.instanceLimit > 0 && instanceCount > context.data.instanceLimit)
            instanceCount = context.data.instanceLimit;
        return instanceCount;
    }
}