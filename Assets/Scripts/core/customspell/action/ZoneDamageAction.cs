using UnityEngine;

public class ZoneDamageAction : ISpellAction {
    private readonly float _damagePerSecond;
    private float _accumulator;

    public ZoneDamageAction(float dps) {
        _damagePerSecond = dps;
    }

    public override void Apply(ISpellContext context, SpellEvent evt) {
        if (evt is not OnZoneStayEvent stay)
            return;

        _accumulator += context.DeltaTime;
        if (_accumulator < 1f) return;
        Debug.Log($"Applying _____________________________ {stay.Target.name}");
        if (!DamageUtils.TryGetOwnerFromCollider(stay.Target, out var damageable, out ulong owner)) return;

        base.Apply(context, evt);
        _accumulator = 0f;
        damageable.TakeDamage("", context.OwnerId, _damagePerSecond);
    }
}