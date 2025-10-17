using System;
using System.Collections;
using UnityEngine;

public class HitScanSpawn : ISpawnStrategy {
    private const float maxDistance = 50f;

    private readonly float delay;

    public HitScanSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(SpellManager manager, SpellData spell) {
        var projCount = (int)Math.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount));
        for (int i = 0; i < projCount; i++) {
            Vector3 groundPos = GetGroundPosition(manager.spellCastPoint);
            Quaternion rot = manager.spellCastPoint.rotation;

            manager.SpawnProjectile(spell, groundPos, rot);

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private Vector3 GetGroundPosition(Transform origin) {
        Vector3 rayStart = origin.position;
        Vector3 direction = origin.forward;

        if (Physics.Raycast(rayStart, direction, out var hit, maxDistance)) {
            return hit.point;
        }

        return origin.position + origin.forward * maxDistance;
    }
}