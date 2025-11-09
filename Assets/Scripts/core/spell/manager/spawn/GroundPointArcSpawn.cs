using System;
using System.Collections;
using UnityEngine;

public class GroundPointArcSpawn : ISpawnStrategy {
    private readonly int terrainLayer = LayerMask.NameToLayer("Terrain");

    private readonly float _delay;
    private float _maxDistance = 50f;

    public GroundPointArcSpawn(float delay = 0f) {
        _delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager,
        SpellData spell,
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        if (manager == null || spell == null) yield break;

        _maxDistance = spell.raycastMaxDistance;
        var angleStep = spell.arcAngleStep;
        Transform castPoint = manager.spellCastPoint;
        int projCount = Mathf.Max(1,
            (int)Mathf.Floor(spell.projCount * manager.statSystem.Stats.GetFinal(StatType.ProjectileCount)));

        float startAngle = -((projCount - 1) * angleStep) / 2f;

        for (int i = projCount - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            // Направление для конкретного снаряда
            Quaternion rotation = castPoint.rotation * Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;
            // Кастим луч по направлению взгляда
            Vector3 groundPos = GetGroundPosition(castPoint.position, direction);

            Vector3 euler = rotation.eulerAngles;
            euler.x = 0f;
            euler.z = 0f;
            Quaternion flatRotation = Quaternion.Euler(euler);
            onSpawn(spell, groundPos, flatRotation, (int)angle);

            if (_delay > 0f && i > 0)
                yield return new WaitForSeconds(_delay);
        }
    }

    private Vector3 GetGroundPosition(Vector3 origin, Vector3 direction) {
        int mask = 1 << terrainLayer;

        // Каст вперёд и вниз по поверхности
        if (Physics.Raycast(origin, direction, out var hit, _maxDistance, mask)) {
            origin = hit.point + hit.normal * 0.3f;
            if (Physics.Raycast(origin, Vector3.down, out var hit2, _maxDistance, mask)) {
                return hit2.point + hit2.normal * 0.3f;
            }
        }

        if (Physics.Raycast(origin + direction * _maxDistance, Vector3.down, out hit, _maxDistance, mask)) {
            return hit.point + hit.normal * 0.3f;
        }

        return new Vector3(1000, 0, 0);
    }
}