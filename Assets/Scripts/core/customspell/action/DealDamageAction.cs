public class DealDamageAction : ISpellAction {
    private readonly float _damage;

    public DealDamageAction(float damage) {
        _damage = damage;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit) return;
        base.Apply(context, evt);
        if (hit.Target.TryGetComponent<Damageable>(out var damageable)) {
            damageable.TakeDamage("", context.OwnerId, _damage);
        }
    }
}