public abstract class PointPhysicsOnHitActionBase : PointPhysicsActionBase {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        var def = context.Spell.knockback;
        if (def == null) return;

        if (!TryResolveTarget(context, hit.ShapeHit.Target, out var target))
            return;

        ApplyResolved(context, hit, def, target);
    }

    protected abstract void ApplyResolved(
        ISpellContext context,
        OnHitEvent hit,
        KnockbackDefinition def,
        ResolvedPhysicsTarget target
    );
}

