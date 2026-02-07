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
        context.Lifetime -= delta;
        var hits = _shape.Query();

        foreach (var hit in hits) {
            var hitEvent = new OnHitEvent {
                Target = hit.Target.gameObject,
                Point = hit.Point,
                Normal = hit.Normal,
                Outcome = HitOutcome.Destroy
            };

            HandleEvent(hitEvent);
            if ((hitEvent.Outcome & HitOutcome.Destroy) != 0) {
                context.View.Kill();
                return;
            }
        }
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}