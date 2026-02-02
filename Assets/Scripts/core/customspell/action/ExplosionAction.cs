using Unity.Netcode;
using UnityEngine;

public class ExplosionAction : ISpellAction {
    private readonly float _radius;
    private readonly float _damage;

    public ExplosionAction(float radius, float damage) {
        _radius = radius;
        _damage = damage;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnHitEvent hit)
            return;
        base.Apply(context, evt);

        var colliders = Physics.OverlapSphere(
            hit.Point,
            _radius
        );

        foreach (var col in colliders) {
            if (col.TryGetComponent<Damageable>(out var damageable)) {
                damageable.TakeDamage("", context.OwnerId, _damage);
            }
        }
    }
}