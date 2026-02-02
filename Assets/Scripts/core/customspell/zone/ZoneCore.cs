using System.Collections.Generic;
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
        var hits = new List<ShapeHit>(_shape.Query(_ctx, result));

        var current = new HashSet<GameObject>();
        foreach (var hit in hits)
            current.Add(hit.Target);

        foreach (var enter in current) {
            if (!_inside.Contains(enter))
                HandleEvent(new OnZoneEnterEvent(enter));
        }

        foreach (var hit in hits)
            HandleEvent(new OnZoneStayEvent(hit.Target));

        foreach (var exit in _inside) {
            if (!current.Contains(exit))
                HandleEvent(new OnZoneExitEvent(exit));
        }

        _inside = current;
    }

    public void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(_ctx, evt);
    }
}