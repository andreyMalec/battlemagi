using System;
using System.Collections;
using UnityEngine;

public class HitScanSpawn : ISpawnStrategy {
    private readonly float delay;
    private float _maxDistance = 50f;

    public HitScanSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager,
        SpellData spell,
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        _maxDistance = spell.raycastMaxDistance;
        var projCount = ISpawnStrategy.ProjCount(manager, spell);
        for (int i = 0; i < projCount; i++) {
            Vector3 groundPos = GetGroundPosition(manager.spellCastPoint);
            Quaternion rot = manager.spellCastPoint.rotation;

            onSpawn(spell, groundPos, rot, i);

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private Vector3 GetGroundPosition(Transform origin) {
        Vector3 rayStart = origin.position;
        Vector3 direction = origin.forward;

        if (Physics.Raycast(rayStart, direction, out var hit, _maxDistance)) {
            return hit.point;
        }

        return origin.position + origin.forward * _maxDistance;
    }
}