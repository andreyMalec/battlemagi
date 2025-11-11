using System;
using System.Collections;
using UnityEngine;

public class DirectSpawn : ISpawnStrategy {
    private readonly float delay;

    public DirectSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager, 
        SpellData spell, 
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        var projCount = ISpawnStrategy.ProjCount(manager, spell);
        for (int i = 0; i < projCount; i++) {
            Vector3 spawnPosition = manager.spellCastPoint.position;
            Quaternion spawnRotation = manager.spellCastPoint.rotation;

            onSpawn(spell, spawnPosition, spawnRotation, i);

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}