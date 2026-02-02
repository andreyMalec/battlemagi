using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ZoneDamageAction : ISpellAction {
    private readonly float _damagePerSecond;
    private float _accumulator;
    private readonly List<ulong> _damagedThisTick = new();

    public ZoneDamageAction(float dps) {
        _damagePerSecond = dps;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay)
            return;

        _accumulator += stay.DeltaTime;
        if (_accumulator < 1f) return;
        _accumulator = 0f;
        _damagedThisTick.Clear();
        foreach (var target in stay.Targets) {
            if (!DamageUtils.TryGetOwnerFromCollider(target, out var damageable, out ulong owner)) continue;
            if (!damageable.IsSpawned || damageable.isDead) continue;
            if (_damagedThisTick.Contains(owner)) continue;
            _damagedThisTick.Add(owner);
            base.Apply(context, evt);

            damageable.TakeDamage("", context.OwnerId, _damagePerSecond);
        }
    }
}