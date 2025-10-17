using System;
using System.Collections;
using UnityEngine;

public class DirectDownSpawn : ISpawnStrategy {
    private readonly float delay;

    public DirectDownSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(SpellManager manager, SpellData spell) {
        var projCount = (int)Math.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount));
        for (int i = 0; i < projCount; i++) {
            Vector3 rayStart = manager.spellCastPoint.position;
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, 5f)) {
                manager.SpawnProjectile(spell, hit.point, Quaternion.identity);
            } else {
                Quaternion spawnRotation = manager.spellCastPoint.rotation;
                manager.SpawnProjectile(spell, rayStart, spawnRotation);
            }

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}