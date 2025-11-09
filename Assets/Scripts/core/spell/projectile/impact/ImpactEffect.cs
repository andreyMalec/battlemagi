using System;
using Unity.Netcode;
using UnityEngine;

public class ImpactEffect : ScriptableObject {
    public virtual GameObject OnImpact(BaseSpell spell, SpellData data) {
        var t = spell.transform;
        var pos = t.position;
        var rot = Quaternion.identity;
        if (Physics.Raycast(t.position - t.forward * 0.1f, t.forward, out var hit, 2f)) {
            pos = hit.point;
            rot = ComputeRotation(hit.normal, t.forward);
        }

        var go = Instantiate(data.impactPrefab, pos, rot);
        if (go.TryGetComponent<NetworkObject>(out var netObj)) {
            netObj.SpawnWithOwnership(spell.OwnerClientId);
        } else {
            throw new Exception($"[{data.name}] impact prefab must be a NetworkObject");
        }

        return go;
    }

    private Quaternion ComputeRotation(Vector3 normal, Vector3 direction) {
        var tangent = Vector3.Cross(normal, direction);
        if (tangent.sqrMagnitude < 0.001f)
            tangent = Vector3.Cross(normal, Vector3.up);

        var forward = Vector3.Cross(tangent, normal);
        return Quaternion.LookRotation(forward, normal);
    }
}