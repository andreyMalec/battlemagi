using System.Collections.Generic;

public class ImpulseKnockbackOnHitAction : PointPhysicsOnHitActionBase {
    private readonly HashSet<Damageable> _applied = new();

    protected override void ApplyResolved(
        ISpellContext context,
        OnHitEvent hit,
        KnockbackDefinition def,
        Damageable damageable,
        PlayerPhysics physics,
        FirstPersonMovement movement
    ) {
        if (_applied.Contains(damageable)) return;
        if (def.impulse <= 0f) return;

        var direction = ComputeDirection(physics, hit.Point, def);
        var impulse = direction * def.impulse;
        if (impulse.sqrMagnitude < 0.0001f) return;

        _applied.Add(damageable);
        ApplyImpulse(physics, movement, impulse);
    }
}

