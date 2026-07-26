using System.Collections.Generic;
using UnityEngine;

public class ZoneDamageModuleAction : ISpellAction {
    private float _accumulator;
    private bool _instantDamaged;
    private readonly HashSet<Damageable> _onceDamaged = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;
        if (context.SpellDamage == null) return;

        var shouldLog = GameConfig.SpellDebugLogsEnabled;
        var actionName = shouldLog ? GetType().Name : string.Empty;
        var eventName = shouldLog ? evt.GetType().Name : string.Empty;

        switch (context.SpellDamage.mode) {
            case SpellDamageMode.Instant:
                if (_instantDamaged) return;
                _instantDamaged = true;
                ApplyInstant(context, stay, shouldLog, actionName, eventName);
                break;
            case SpellDamageMode.DamageOverTime:
                ApplyDot(context, stay, shouldLog, actionName, eventName);
                break;
            case SpellDamageMode.OncePerLifetime:
                ApplyOncePerTarget(context, stay, shouldLog, actionName, eventName);
                break;
        }
    }

    private void ApplyInstant(ISpellContext context, OnZoneStayEvent stay, bool shouldLog, string actionName, string eventName) {
        foreach (var t in stay.Targets) {
            if (!stay.TryGetDamageable(t.Target, out var damageable, out var owner))
                continue;

            if (damageable.IsDead) continue;
            if (!DamageRelationship.CanDamage(context, damageable, owner)) continue;

            DealResolved(context, damageable, t.Point, shouldLog, actionName, eventName);
        }
    }

    private void ApplyDot(ISpellContext context, OnZoneStayEvent stay, bool shouldLog, string actionName, string eventName) {
        _accumulator += stay.DeltaTime;
        if (_accumulator < context.SpellDamage.tickInterval) return;
        _accumulator = 0f;

        foreach (var t in stay.Targets)
            Deal(context, stay, t, shouldLog, actionName, eventName);
    }

    private void ApplyOncePerTarget(ISpellContext context, OnZoneStayEvent stay, bool shouldLog, string actionName, string eventName) {
        foreach (var t in stay.Targets) {
            if (!stay.TryGetDamageable(t.Target, out var damageable, out var owner))
                continue;

            if (_onceDamaged.Contains(damageable))
                continue;

            if (damageable.IsDead) continue;
            if (!DamageRelationship.CanDamage(context, damageable, owner)) continue;

            _onceDamaged.Add(damageable);
            DealResolved(context, damageable, t.Point, shouldLog, actionName, eventName);
        }
    }

    private void Deal(ISpellContext context, OnZoneStayEvent stay, ShapeHit hit, bool shouldLog, string actionName, string eventName) {
        if (hit.Target == null) return;
        if (!stay.TryGetDamageable(hit.Target, out var damageable, out var owner)) return;
        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        DealResolved(context, damageable, hit.Point, shouldLog, actionName, eventName);
    }

    private void DealResolved(ISpellContext context, Damageable damageable, Vector3 point, bool shouldLog, string actionName, string eventName) {
        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable, point);
        if (amount <= 0f) return;
        if (shouldLog)
            SpellLog.Log($"SpellAction {actionName} applied to {damageable.name}. Event: {eventName}");
        damageable.TakeDamage(context.Spell.spellName, context.OwnerId, amount,
            Ctx.GetSpellSound(context.Spell), context.SpellDamage.ignoreSoundCooldown);
    }
}