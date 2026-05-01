public class ContinuousPointKnockbackOnHitAction : PointPhysicsOnHitActionBase {
    protected override void ApplyResolved(
        ISpellContext context,
        OnHitEvent hit,
        KnockbackDefinition def,
        ResolvedPhysicsTarget target
    ) {
        if (def.forcePerSecond <= 0f) return;
        if (def.duration <= 0f) return;

        ApplyPointForce(context, target, hit.ShapeHit.Point, def);
    }
}

