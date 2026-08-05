using System.Collections.Generic;
using UnityEngine;

public class ProjectileInstantDamageAction : ISpellAction {
    private readonly HashSet<Damageable> _onceDamaged = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (context.SpellDamage == null) return;

        var shouldLog = GameConfig.SpellDebugLogsEnabled;
        var actionName = shouldLog ? GetType().Name : string.Empty;
        var eventName = shouldLog ? evt.GetType().Name : string.Empty;

        switch (context.SpellDamage.mode) {
            case SpellDamageMode.Instant:
                ApplyInstant(context, hit, shouldLog, actionName, eventName);
                break;
            case SpellDamageMode.OncePerTarget:
                ApplyOncePerTarget(context, hit, shouldLog, actionName, eventName);
                break;
        }
    }

    private void ApplyInstant(
        ISpellContext context,
        OnHitEvent hit,
        bool shouldLog,
        string actionName,
        string eventName
    ) {
        if (!DamageUtils.TryGetOwnerFromCollider(hit.ShapeHit.Target, out var damageable, out var owner))
            return;

        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        DealResolved(context, damageable, hit.ShapeHit.Point, shouldLog, actionName, eventName);
    }

    private void ApplyOncePerTarget(
        ISpellContext context,
        OnHitEvent hit,
        bool shouldLog,
        string actionName,
        string eventName
    ) {
        if (!DamageUtils.TryGetOwnerFromCollider(hit.ShapeHit.Target, out var damageable, out var owner))
            return;

        if (_onceDamaged.Contains(damageable))
            return;

        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        _onceDamaged.Add(damageable);
        DealResolved(context, damageable, hit.ShapeHit.Point, shouldLog, actionName, eventName);
    }

    private void DealResolved(
        ISpellContext context, Damageable damageable, Vector3 point, bool shouldLog, string actionName, string eventName
    ) {
        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable, point);
        if (amount <= 0f) return;
        if (shouldLog)
            SpellLog.Log($"SpellAction {actionName} applied to {damageable.name}. Event: {eventName}");
        damageable.TakeDamage(context.Spell.spellName, context.OwnerId, amount,
            Ctx.GetSpellSound(context.Spell), context.SpellDamage.ignoreSoundCooldown);
    }
}