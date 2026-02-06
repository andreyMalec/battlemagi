using System;
using System.Collections;
using UnityEngine;

public class NewDirectSpawn : IDelayedSpawn {
    public NewDirectSpawn(float delay) : base(delay) {
    }

    public override IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn) {
        var count = ISpellSpawn.InstanceCount(context);
        for (int i = 0; i < count; i++) {
            spawn(context, i);
            if (delay > 0f && i < count - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}