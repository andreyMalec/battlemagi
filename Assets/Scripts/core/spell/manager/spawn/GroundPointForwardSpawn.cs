using System;
using System.Collections;
using UnityEngine;

public class GroundPointForwardSpawn : ISpawnStrategy {
    private readonly int _terrainLayer = LayerMask.NameToLayer("Terrain");

    private readonly float _delay;
    private float _maxDistance = 50f;

    public GroundPointForwardSpawn(float delay = 0f) {
        _delay = delay;
    }

    public IEnumerator Spawn(
        SpellManager manager,
        SpellData spell,
        Action<SpellData, Vector3, Quaternion, int> onSpawn
    ) {
        _maxDistance = spell.raycastMaxDistance;
        var projCount = ISpawnStrategy.ProjCount(manager, spell);
        Transform castPoint = manager.spellCastPoint;

        Vector3 forwardHint = castPoint.forward;
        var (groundPos, groundRot) = GetGroundPose(castPoint.position, forwardHint, 0f);

        Vector3 groundUp = groundRot * Vector3.up;
        Vector3 groundForward = groundRot * Vector3.forward;
        Vector3 groundRight = groundRot * Vector3.right;

        Vector3 localStep = spell.forwardStep;
        Vector3 step = groundRight * localStep.x + groundUp * localStep.y + groundForward * localStep.z;

        for (int i = 0; i < projCount; i++) {
            if (i > 0) {
                groundPos += step;
            }

            onSpawn(spell, groundPos, groundRot * Quaternion.Euler(spell.forwardAngle), i);

            if (_delay > 0f && i < projCount - 1)
                yield return new WaitForSeconds(_delay);
        }
    }

    private (Vector3 position, Quaternion rotation) GetGroundPose(
        Vector3 origin, Vector3 direction, float distanceOffset
    ) {
        int mask = 1 << _terrainLayer;

        Vector3 rayStart = origin + direction * distanceOffset;

        if (Physics.Raycast(rayStart, direction, out var hit, _maxDistance, mask)) {
            origin = hit.point + hit.normal * 0.3f;
            if (Physics.Raycast(origin, Vector3.down, out var hit2, _maxDistance, mask)) {
                return (hit2.point + hit2.normal * 0.3f, RotationFromNormal(direction, hit2.normal));
            }

            return (hit.point + hit.normal * 0.3f, RotationFromNormal(direction, hit.normal));
        }

        if (Physics.Raycast(rayStart + direction * _maxDistance, Vector3.down, out hit, _maxDistance, mask)) {
            return (hit.point + hit.normal * 0.3f, RotationFromNormal(direction, hit.normal));
        }

        if (Physics.Raycast(rayStart + Vector3.up, Vector3.down, out hit, _maxDistance, mask)) {
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