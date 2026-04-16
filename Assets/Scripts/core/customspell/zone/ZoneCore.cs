using System.Collections.Generic;
using UnityEngine;

public class ZoneCore : ISpellCore<ZoneContext> {
    private readonly IShape _shape;
    private readonly List<GameObject> _targets = new();
    private bool _isInitialTick = true;

    public ZoneCore(
        ZoneContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.ZoneCoreTick);
        context.Lifetime -= delta;
        _targets.Clear();
        foreach (var hit in _shape.Query()) {
            if (hit.Target == null)
                continue;

            _targets.Add(hit.Target.gameObject);
        }

        HandleEvent(new OnZoneStayEvent(_targets, delta, _isInitialTick));
        _isInitialTick = false;
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}