using System.Collections;
using UnityEngine;

public class DirectSpawn : ISpawnStrategy {
    private readonly float delay;

    public DirectSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(SpellManager manager, SpellData spell) {
        for (int i = 0; i < spell.projCount; i++) {
            Vector3 spawnPosition = manager.spellCastPoint.position;
            Quaternion spawnRotation = manager.spellCastPoint.rotation;

            manager.SpawnProjectile(spell, spawnPosition, spawnRotation);

            if (delay > 0f && i < spell.projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}