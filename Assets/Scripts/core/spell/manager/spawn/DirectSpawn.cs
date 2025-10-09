using System;
using System.Collections;
using UnityEngine;

public class DirectSpawn : ISpawnStrategy {
    private readonly float delay;

    public DirectSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(SpellManager manager, SpellData spell) {
        var projCount = (int)Math.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount));
        for (int i = 0; i < projCount; i++) {
            Vector3 spawnPosition = manager.spellCastPoint.position;
            Quaternion spawnRotation = manager.spellCastPoint.rotation;

            manager.SpawnProjectile(spell, spawnPosition, spawnRotation);

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}