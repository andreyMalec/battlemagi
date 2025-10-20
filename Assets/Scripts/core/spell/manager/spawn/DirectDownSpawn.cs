using System;
using System.Collections;
using UnityEngine;

public class DirectDownSpawn : ISpawnStrategy {
    private readonly float delay;

    public DirectDownSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager, 
        SpellData spell, 
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        var projCount = (int)Math.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount));
        for (int i = 0; i < projCount; i++) {
            Vector3 rayStart = manager.spellCastPoint.position;
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, 5f)) {
                onSpawn(spell, hit.point, Quaternion.identity, i);
            } else {
                Quaternion spawnRotation = manager.spellCastPoint.rotation;
                onSpawn(spell, rayStart, spawnRotation, i);
            }

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}