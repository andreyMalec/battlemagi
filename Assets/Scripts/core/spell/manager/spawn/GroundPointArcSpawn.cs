using System;
using System.Collections;
using UnityEngine;

public class GroundPointArcSpawn : ISpawnStrategy {
    private readonly int _terrainLayer = LayerMask.NameToLayer("Terrain");

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
        var projCount = ISpawnStrategy.ProjCount(manager, spell);
        float startAngle = -((projCount - 1) * angleStep) / 2f;

        for (int i = projCount - 1; i >= 0; i--) {
            float angle = startAngle + angleStep * i;

            Quaternion rotation = castPoint.rotation * Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;

            var (groundPos, groundRot) = GetGroundPose(castPoint.position, direction);
            onSpawn(spell, groundPos, groundRot, (int)angle);

            if (_delay > 0f && i > 0)
                yield return new WaitForSeconds(_delay);
        }
    }

    private (Vector3 position, Quaternion rotation) GetGroundPose(Vector3 origin, Vector3 direction) {
        int mask = 1 << _terrainLayer;

        if (Physics.Raycast(origin, direction, out var hit, _maxDistance, mask)) {
            origin = hit.point + hit.normal * 0.3f;
            if (Physics.Raycast(origin, Vector3.down, out var hit2, _maxDistance, mask)) {
                return (hit2.point + hit2.normal * 0.3f, RotationFromNormal(direction, hit2.normal));
            }

            return (hit.point + hit.normal * 0.3f, RotationFromNormal(direction, hit.normal));
        }

        if (Physics.Raycast(origin + direction * _maxDistance, Vector3.down, out hit, _maxDistance, mask)) {
            return (hit.point + hit.normal * 0.3f, RotationFromNormal(direction, hit.normal));
        }

        if (Physics.Raycast(origin + Vector3.up, Vector3.down, out hit, _maxDistance, mask)) {
            return (hit.point + hit.normal * 0.3f, RotationFromNormal(direction, hit.normal));
        }

        return (new Vector3(1000, 0, 0), Quaternion.identity);
    }

    private static Quaternion RotationFromNormal(Vector3 forwardHint, Vector3 normal) {
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(forwardHint, normal);
        if (forwardOnPlane.sqrMagnitude < 0.0001f) forwardOnPlane = Vector3.ProjectOnPlane(Vector3.forward, normal);
        return Quaternion.LookRotation(forwardOnPlane.normalized, normal);
    }
}