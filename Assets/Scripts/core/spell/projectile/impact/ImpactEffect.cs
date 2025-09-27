using UnityEngine;

public class ImpactEffect : IProjectileImpact {
    private readonly SpellData data;
    private readonly SpellProjectile projectile;

    public ImpactEffect(SpellProjectile p, SpellData d) {
        projectile = p;
        data = d;
    }

    public void OnImpact(Collider other) {
        if (data.impactPrefab == null) return;

        var t = projectile.transform;
        // Находим точку удара
        if (Physics.Raycast(t.position - t.forward * 0.1f, t.forward, out var hit, 2f)) {
            var rot = ComputeRotation(hit.normal, t.forward);
            projectile.SpawnImpactServerRpc(data.id, hit.point, rot, projectile.OwnerClientId);
        } else {
            projectile.SpawnImpactServerRpc(data.id, t.position, Quaternion.identity, projectile.OwnerClientId);
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