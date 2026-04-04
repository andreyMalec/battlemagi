public abstract class PointPhysicsOnHitActionBase : PointPhysicsActionBase {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        var def = context.Spell.knockback;
        if (def == null) return;

        if (!TryResolveTarget(context, hit.Target, out var damageable, out var physics, out var movement))
            return;

        ApplyResolved(context, hit, def, damageable, physics, movement);
    }

    protected abstract void ApplyResolved(
        ISpellContext context,
        OnHitEvent hit,
        KnockbackDefinition def,
        Damageable damageable,
        PlayerPhysics physics,
        FirstPersonMovement movement
    );
}

