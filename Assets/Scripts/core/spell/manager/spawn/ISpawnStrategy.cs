using System;
using System.Collections;
using UnityEngine;

public interface ISpawnStrategy {
    IEnumerator Spawn(SpellManager manager, SpellData spell, Action<SpellData, Vector3, Quaternion, int> onSpawn);

    public static int ProjCount(SpellManager manager, SpellData spell) {
        var projCount = (int)Math.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount));
        if (spell.instanceLimit > 0 && projCount > spell.instanceLimit)
            projCount = spell.instanceLimit;
        return projCount;
    }
}