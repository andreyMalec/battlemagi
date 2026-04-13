using System.Linq;

public class ZoneCore : ISpellCore<ZoneContext> {
    private readonly IShape _shape;
    private bool _isInitialTick = true;

    public ZoneCore(
        ZoneContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        context.Lifetime -= delta;
        var hits = _shape.Query().ToList();

        HandleEvent(new OnZoneStayEvent(hits.Map(it => it.Target.gameObject), delta, _isInitialTick));
        _isInitialTick = false;
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}