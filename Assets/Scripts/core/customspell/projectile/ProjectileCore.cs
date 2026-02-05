public class ProjectileCore : ISpellCore<ProjectileContext> {
    private readonly IShape _shape;
    private bool _sentLifetimeEnding;

    public ProjectileCore(
        ProjectileContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    public override void Tick(float delta) {
        if (!_sentLifetimeEnding && context.Lifetime > 0f && context.Lifetime <= BeforeEndThreshold) {
            _sentLifetimeEnding = true;
            HandleEvent(new OnLifetimeEndingEvent { remaining = context.Lifetime });
        }

        context.Lifetime -= delta;
        if (context.Lifetime <= 0f) {
            context.View.Kill();
            return;
        }

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