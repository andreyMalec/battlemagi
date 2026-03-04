public class ProjectileInstantDamageAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (context.SpellDamage == null) return;
        if (context.SpellDamage.mode is not SpellDamageMode.Instant) return;

        if (!NewDamageUtils.TryGetOwnerFromCollider(hit.Target, out var damageable, out var owner))
            return;

        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable);
        if (amount <= 0f) return;

        base.Apply(context, evt);
        damageable.TakeDamage("", context.OwnerId, amount);
    }
}