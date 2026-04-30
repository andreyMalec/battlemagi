public class ProjectileCore : ISpellCore<ProjectileContext> {
    private readonly IShape _shape;

    public ProjectileCore(
        ProjectileContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.ProjectileCoreTick);
        context.Lifetime -= delta;
        var hits = _shape.Query();

        foreach (var hit in hits) {
            var hitEvent = new OnHitEvent {
                ShapeHit = hit,
                Outcome = HitOutcome.Destroy
            };

            HandleEvent(hitEvent);
            if ((hitEvent.Outcome & HitOutcome.Destroy) != 0) {
                context.View.Kill(context);
                return;
            }
        }
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}