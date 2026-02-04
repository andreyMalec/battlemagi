using System.Collections.Generic;

public class ZoneCore : ISpellCore<ZoneContext> {
    private readonly IShape _shape;

    public ZoneCore(
        ZoneContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    public override void Tick(float delta) {
        context.Lifetime -= delta;
        if (context.Lifetime <= 0f) {
            context.View.Kill();
            return;
        }

        var hits = _shape.Query();

        HandleEvent(new OnZoneStayEvent(hits.Map(it => it.Target.gameObject), delta));
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}