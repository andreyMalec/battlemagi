using System.Collections.Generic;
using UnityEngine;

public class ZoneDamageModuleAction : ISpellAction {
    private float _accumulator;
    private bool _instantDamaged;
    private readonly HashSet<Damageable> _onceDamaged = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;
        if (context.SpellDamage == null) return;

        switch (context.SpellDamage.mode) {
            case SpellDamageMode.Instant:
                if (_instantDamaged) return;
                _instantDamaged = true;
                ApplyInstant(context, stay, evt);
                break;
            case SpellDamageMode.DamageOverTime:
                ApplyDot(context, stay, evt);
                break;
            case SpellDamageMode.OncePerLifetime:
                ApplyOncePerTarget(context, stay, evt);
                break;
        }
    }

    private void ApplyInstant(ISpellContext context, OnZoneStayEvent stay, SpellEvent evt) {
        foreach (var t in stay.Targets) {
            if (!DamageUtils.TryGetOwnerFromCollider(t, out var damageable, out var owner))
                continue;

            if (damageable.IsDead) continue;
            if (!DamageRelationship.CanDamage(context, damageable, owner)) continue;

            DealResolved(context, damageable, evt);
        }
    }

    private void ApplyDot(ISpellContext context, OnZoneStayEvent stay, SpellEvent evt) {
        _accumulator += stay.DeltaTime;
        if (_accumulator < context.SpellDamage.tickInterval) return;
        _accumulator = 0f;

        foreach (var t in stay.Targets)
            Deal(context, t, evt);
    }

    private void ApplyOncePerTarget(ISpellContext context, OnZoneStayEvent stay, SpellEvent evt) {
        foreach (var t in stay.Targets) {
            if (!DamageUtils.TryGetOwnerFromCollider(t, out var damageable, out var owner))
                continue;

            if (_onceDamaged.Contains(damageable))
                continue;

            if (damageable.IsDead) continue;
            if (!DamageRelationship.CanDamage(context, damageable, owner)) continue;

            _onceDamaged.Add(damageable);
            DealResolved(context, damageable, evt);
        }
    }

    private void Deal(ISpellContext context, GameObject targetGo, SpellEvent evt) {
        if (targetGo == null) return;
        if (!DamageUtils.TryGetOwnerFromCollider(targetGo, out var damageable, out var owner)) return;
        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        DealResolved(context, damageable, evt);
    }

    private void DealResolved(ISpellContext context, Damageable damageable, SpellEvent evt) {
        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable);
        if (amount <= 0f) return;
        Debug.Log($"SpellAction {GetType().Name} applied to {damageable.name}. Event: {evt.GetType().Name}");
        damageable.TakeDamage(context.Spell.name, context.OwnerId, amount,
            SpellPrefabDatabase.Instance.Sound(context.Spell), context.SpellDamage.ignoreSoundCooldown);
    }
}