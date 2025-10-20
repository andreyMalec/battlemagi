using System;
using System.Collections;
using UnityEngine;

public interface ISpawnStrategy {
    IEnumerator Spawn(SpellManager manager, SpellData spell, Action<SpellData, Vector3, Quaternion, int> onSpawn);
}