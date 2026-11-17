using UnityEngine;

public class SelfCore : ISpellCore<SelfContext> {
    public SelfCore(
        SelfContext ctx,
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