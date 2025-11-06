using System.Collections.Generic;
using UnityEngine;

public class DotDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    // Track last damage time per owner to prevent multiple colliders causing multiple ticks
    private readonly Dictionary<ulong, float> _lastDamageTime = new Dictionary<ulong, float>();

    public DotDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public void OnHit(Collider other) {
    }

    public void OnStay(Collider other) {
        if (!DamageUtils.TryGetOwnerFromCollider(other, out var damageable, out var owner))
            return;

        if (_lastDamageTime.TryGetValue(owner, out var last) && Time.time - last < data.tickInterval)
            return;

        var applied = DamageUtils.TryApplyDamage(spell, data, damageable, other);
        if (applied != ulong.MaxValue) {
            _lastDamageTime[owner] = Time.time;
        }
    }
}