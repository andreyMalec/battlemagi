using System;
using System.Collections;
using UnityEngine;

public abstract class IDelayedSpawn : ISpellSpawn {
    protected readonly float delay;

    protected IDelayedSpawn(float delay) {
        this.delay = delay;
    }

    public abstract IEnumerator Request(SpawnContext context, Action<SpawnContext, int> spawn);
}