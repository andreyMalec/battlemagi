public class ContinuousPointKnockbackOnHitAction : PointPhysicsOnHitActionBase {
    protected override void ApplyResolved(
        ISpellContext context,
        OnHitEvent hit,
        KnockbackDefinition def,
        Damageable damageable,
        PlayerPhysics physics,
        FirstPersonMovement movement
    ) {
        if (def.forcePerSecond <= 0f) return;
        if (def.duration <= 0f) return;

        ApplyPointForce(context, physics, movement, hit.Point, def);
    }
}

