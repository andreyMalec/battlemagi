using UnityEngine;

public class ZoneCore : ISpellCore<ZoneContext> {
    private readonly IShape _shape;

    public ZoneCore(
        ZoneContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        context.Lifetime -= delta;
        var hits = _shape.Query();

        foreach (var hit in hits) {
            Debug.Log($"_______________ hit {hit.Target.gameObject.name} at point {hit.Point} with normal {hit.Normal}");
        }
        HandleEvent(new OnZoneStayEvent(hits.Map(it => it.Target.gameObject), delta));
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}