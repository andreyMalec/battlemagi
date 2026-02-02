using System.Collections.Generic;
using UnityEngine;

public class ZoneCore : ISpellCore {
    private readonly ZoneContext _ctx;
    private readonly IShape _shape;

    public ZoneCore(
        ZoneContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _ctx = ctx;
        _shape = shape;
    }

    public override void Tick(float delta) {
        _ctx.Lifetime -= delta;
        if (_ctx.Lifetime <= 0f) {
            _ctx.View.Kill();
            return;
        }

        var hits = _shape.Query();

        HandleEvent(new OnZoneStayEvent(hits.Map(it => it.Target.gameObject), delta));
    }
}