using System;
using System.Collections;
using UnityEngine;

public class DirectDownSpawn : ISpawnStrategy {
    private readonly int terrainLayer = LayerMask.NameToLayer("Terrain");
    private readonly float delay;

    public DirectDownSpawn(float delay = 0f) {
        this.delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager,
        SpellData spell,
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        var mask = 1 << terrainLayer;
        var projCount = ISpawnStrategy.ProjCount(manager, spell);
        for (int i = 0; i < projCount; i++) {
            Vector3 rayStart = manager.spellCastPoint.position;
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, spell.raycastMaxDistance, mask)) {
                onSpawn(spell, hit.point + Vector3.up * 0.01f, ComputeRotation(hit.normal, Vector3.down), i);
            } else {
                Quaternion spawnRotation = manager.spellCastPoint.rotation;
                onSpawn(spell, rayStart, spawnRotation, i);
            }

            if (delay > 0f && i < projCount - 1) {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private Quaternion ComputeRotation(Vector3 normal, Vector3 direction) {
        var tangent = Vector3.Cross(normal, direction);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        var forward = Vector3.Cross(tangent, normal);
        return Quaternion.LookRotation(forward, normal);
    }
}