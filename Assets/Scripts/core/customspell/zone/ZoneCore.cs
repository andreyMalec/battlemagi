using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ZoneCore : ISpellCore {
    private readonly ZoneContext _ctx;
    private readonly IShape<IZoneContext, ZoneVolume> _shape;
    private readonly SpellTrigger[] _triggers;
    private readonly float _duration;
    private HashSet<GameObject> _inside = new();

    public ZoneCore(
        ZoneContext ctx,
        IShape<IZoneContext, ZoneVolume> shape,
        SpellTrigger[] triggers,
        float duration
    ) {
        _ctx = ctx;
        _shape = shape;
        _triggers = triggers;
        _duration = duration;
    }

    public void Tick(float delta) {
        _ctx.Tick(delta);

        if (_ctx.Age >= _duration) {
            _ctx.View.Kill();
            return;
        }

        var result = _shape.Sample(_ctx);
        var hits = _shape.Query(_ctx, result);
        var current = hits.Select(h => h.Target).ToHashSet();
        foreach (var enter in current.Except(_inside))
            HandleEvent(new OnZoneStayEvent(enter));
        foreach (var hit in hits)
            HandleEvent(new OnZoneStayEvent(hit.Target));
        foreach (var exit in _inside.Except(current))
            HandleEvent(new OnZoneStayEvent(exit));

        _inside = current;
    }

    public void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(_ctx, evt);
    }
}