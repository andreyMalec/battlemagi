using System.Collections.Generic;

public class ZoneKnockbackAction : PointPhysicsActionBase {
    private readonly HashSet<Damageable> _impulsed = new();

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay) return;
        var def = context.Spell.knockback;
        if (def == null) return;

        var point = context.Movement.Transform.position;
        foreach (var target in stay.Targets) {
            if (!TryResolveTarget(context, target, out var damageable, out var physics, out var movement))
                continue;

            switch (def.mode) {
                case SpellKnockbackMode.Impulse:
                    ApplyImpulse(damageable, physics, movement, point, def);
                    break;
                case SpellKnockbackMode.Continuous:
                    ApplyPointForce(context, physics, movement, point, def);
                    break;
            }
        }
    }

    private void ApplyImpulse(
        Damageable damageable,
        PlayerPhysics physics,
        FirstPersonMovement movement,
        UnityEngine.Vector3 point,
        KnockbackDefinition def
    ) {
        if (_impulsed.Contains(damageable)) return;
        if (def.impulse <= 0f) return;

        var direction = ComputeDirection(physics, point, def);
        var impulse = direction * def.impulse;
        if (impulse.sqrMagnitude < 0.0001f) return;

        _impulsed.Add(damageable);
        ApplyImpulse(physics, movement, impulse);
    }
}

