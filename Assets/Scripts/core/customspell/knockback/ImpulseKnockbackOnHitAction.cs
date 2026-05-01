using System.Collections.Generic;
using UnityEngine;

public class ImpulseKnockbackOnHitAction : PointPhysicsOnHitActionBase {
    private readonly HashSet<Object> _applied = new();

    protected override void ApplyResolved(
        ISpellContext context,
        OnHitEvent hit,
        KnockbackDefinition def,
        ResolvedPhysicsTarget target
    ) {
        if (_applied.Contains(target.Key)) return;
        if (def.impulse <= 0f) return;

        var direction = ComputeDirection(target.Transform, hit.ShapeHit.Point, def);
        var impulse = direction * def.impulse;
        if (impulse.sqrMagnitude < 0.0001f) return;

        _applied.Add(target.Key);
        ApplyImpulse(target, impulse);
    }
}

