public class ProjectileInstantDamageAction : ISpellAction {
    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (context.SpellDamage == null) return;
        if (context.SpellDamage.mode is not SpellDamageMode.Instant) return;

        if (!DamageUtils.TryGetOwnerFromCollider(hit.ShapeHit.Target, out var damageable, out var owner))
            return;

        if (damageable.IsDead) return;
        if (!DamageRelationship.CanDamage(context, damageable, owner)) return;

        var amount = DamageResolver.Resolve(context.SpellDamage, context, damageable, hit.ShapeHit.Point);
        if (amount <= 0f) return;

        base.Apply(context, evt);
        damageable.TakeDamage(context.Spell.spellName, context.OwnerId, amount,
            SpellPrefabDatabase.Instance.Sound(context.Spell), context.SpellDamage.ignoreSoundCooldown);
    }
}