using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SpellProjectile : BaseSpell {
    private IProjectileImpact impact;

    public override void Initialize(SpellData data, float damageMulti, int index) {
        base.Initialize(data, damageMulti, index);

        impact = new ImpactEffect(this, spellData);
    }

    protected override void OnHit(Collider other) {
        impact.OnImpact(other);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnImpactServerRpc(int spellId, Vector3 position, Quaternion quaternion, ulong ownerId) {
        var spell = SpellDatabase.Instance.GetSpell(spellId);
        var go = Instantiate(spell.impactPrefab, position, quaternion);
        if (go.TryGetComponent<NetworkObject>(out var netObj)) {
            netObj.SpawnWithOwnership(ownerId);
        } else {
            throw new Exception($"[{spell.name}] impact prefab must be a NetworkObject");
        }
    }
}