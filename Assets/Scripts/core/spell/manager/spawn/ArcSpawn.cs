using System;
using System.Collections;
using UnityEngine;

public class ArcSpawn : ISpawnStrategy {
    private readonly float delay;

    public ArcSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager,
        SpellData spell,
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        var angleStep = spell.arcAngleStep;
        Vector3 origin = manager.spellCastPoint.position;
        var projCount = ISpawnStrategy.ProjCount(manager, spell);
        float startAngle = -((projCount - 1) * angleStep) / 2f;

        for (int i = projCount - 1; i >= 0; i--) {
            var angle = startAngle + angleStep * i;
            Quaternion rot = manager.spellCastPoint.rotation * Quaternion.Euler(0, angle, 0);
            onSpawn(spell, origin, rot, (int)angle);

            if (delay > 0f && i > 0) {
                yield return new WaitForSeconds(delay);
            }
        }
    }
}