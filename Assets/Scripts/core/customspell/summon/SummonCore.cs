using UnityEngine;

public class SummonCore : ISpellCore<SummonContext> {
    public SummonCore(
        SummonContext ctx,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
    }

    protected override void TickInner(float delta) {
        context.Lifetime -= delta;
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}