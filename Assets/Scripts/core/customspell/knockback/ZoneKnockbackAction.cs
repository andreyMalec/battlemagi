using System.Collections.Generic;
using UnityEngine;

public class ZoneKnockbackAction : PointPhysicsActionBase {
    private readonly HashSet<Object> _impulsed = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;
        var def = context.Spell.knockback;
        if (def == null) return;
        if (def.mode is SpellKnockbackMode.Impulse && !stay.IsInitial) return;

        var point = context.Movement.Transform.position;
        foreach (var hit in stay.Targets) {
            if (!TryResolveTarget(context, hit.Target, out var target))
                continue;

            switch (def.mode) {
                case SpellKnockbackMode.Impulse:
                    ApplyImpulse(context, target, point, def);
                    break;
                case SpellKnockbackMode.Continuous:
                    ReportLaunchIfNeeded(context, target);
                    ApplyPointForce(context, target, point, def);
                    break;
            }
        }
    }

    private void ApplyImpulse(
        ISpellContext context,
        ResolvedPhysicsTarget target,
        Vector3 point,
        KnockbackDefinition def
    ) {
        if (_impulsed.Contains(target.Key)) return;
        if (def.impulse <= 0f) return;

        var direction = ComputeDirection(target.Transform, point, def);
        var impulse = direction * def.impulse;
        if (impulse.sqrMagnitude < 0.0001f) return;

        _impulsed.Add(target.Key);
        ApplyImpulse(target, impulse);
        ReportLaunchIfNeeded(context, target);
    }
}