using System;
using System.Collections;
using UnityEngine;

public class ArcSpawn : ISpawnStrategy {
    private readonly float angleStep;
    private readonly float delay;

    public ArcSpawn(float angleStep = 15f, float delay = 0f) {
        this.angleStep = angleStep;
        this.delay = delay;
    }

    public IEnumerator Spawn(SpellManager manager, SpellData spell) {
        Vector3 origin = manager.spellCastPoint.position;
        var projCount = (int)Math.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount));
        float startAngle = -((projCount - 1) * angleStep) / 2f;

        for (int i = projCount - 1; i >= 0; i--) {
            Quaternion rot = manager.spellCastPoint.rotation * Quaternion.Euler(0, startAngle + angleStep * i, 0);
            manager.SpawnProjectile(spell, origin, rot);

            if (delay > 0f && i > 0) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}