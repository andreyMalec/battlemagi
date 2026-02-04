public class DealDamageAction : ISpellAction {
    private readonly float _damage;

    public DealDamageAction(float damage) {
        _damage = damage;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        if (!DamageUtils.TryGetOwnerFromCollider(hit.Target, out var damageable, out ulong owner)) return;
        if (!damageable.IsSpawned || damageable.isDead) return;
        base.Apply(context, evt);
        damageable.TakeDamage("", context.OwnerId, _damage);
    }
}