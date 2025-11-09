using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DotDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    private float _tickTimer;

    private readonly List<Damageable> _inside = new();

    public DotDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
        _tickTimer = d.tickInterval;
    }

    public bool OnEnter(Collider other) {
        if (!DamageUtils.TryGetOwnerFromCollider(other, out var damageable, out _))
            return false;
        _inside.Add(damageable);
        return false;
    }

    public bool OnExit(Collider other) {
        if (!DamageUtils.TryGetOwnerFromCollider(other, out var damageable, out _))
            return false;
        _inside.Remove(damageable);
        return false;
    }

    public bool Update() {
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= data.tickInterval) {
            _tickTimer = 0f;

            var toIterate = _inside.ToList();

            var damagedThisTick = new List<ulong>();
            foreach (var damageable in toIterate) {
                if (!damageable.IsSpawned || damageable.isDead) continue;
                var owner = damageable.OwnerClientId;
                if (damagedThisTick.Contains(owner))
                    continue;
                damagedThisTick.Add(owner);

                DamageUtils.TryApplyDamage(spell, data, damageable, damageable.GetComponent<Collider>());
            }

            return true;
        }

        return false;
    }
}