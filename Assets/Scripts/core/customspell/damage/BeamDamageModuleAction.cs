using System.Collections.Generic;
using UnityEngine;

public class BeamDamageModuleAction : ISpellAction {
    private float _accumulator;
    private readonly HashSet<Damageable> _onceDamaged = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (context.SpellDamage == null) return;

        switch (context.SpellDamage.mode) {
            case SpellDamageMode.DamageOverTime:
                ApplyDot(context, evt);
                break;
            case SpellDamageMode.OncePerLifetime:
                ApplyOncePerTarget(context, evt);
                break;
        }
    }

    private void ApplyDot(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;

        _accumulator += context.DeltaTime;
        if (_accumulator < context.SpellDamage.tickInterval) return;
        _accumulator = 0f;

        Deal(context, hit.ShapeHit, evt);
    }

    private void ApplyOncePerTarget(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;

        if (!DamageUtils.TryGetOwnerFromCollider(hit.ShapeHit.Target, out var damageable, out var owner))
            return;

        if (_onceDamaged.Contains(damageable))
            return;

        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        _onceDamaged.Add(damageable);

        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable, hit.ShapeHit.Point);
        if (amount <= 0f) return;

        base.Apply(context, evt);
        damageable.TakeDamage(context.Spell.name, context.OwnerId, amount,
            SpellPrefabDatabase.Instance.Sound(context.Spell), context.SpellDamage.ignoreSoundCooldown);
    }

    private void Deal(ISpellContext context, ShapeHit hit, SpellEvent evt) {
        if (hit.Target == null) return;
        if (!DamageUtils.TryGetOwnerFromCollider(hit.Target, out var damageable, out var owner)) return;
        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable, hit.Point);
        if (amount <= 0f) return;

        base.Apply(context, evt);
        damageable.TakeDamage(context.Spell.name, context.OwnerId, amount,
            SpellPrefabDatabase.Instance.Sound(context.Spell), context.SpellDamage.ignoreSoundCooldown);
    }
}