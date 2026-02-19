using System.Collections.Generic;

public class BeamCore : ISpellCore<BeamContext> {
    private readonly IShape _shape;
    private readonly HashSet<object> _inside = new();
    private bool _started;

    public BeamCore(
        BeamContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        context.Lifetime -= delta;
        if (!_started) {
            _started = true;
            HandleEvent(new OnBeamStartEvent());
        }

        HandleEvent(new OnBeamTickEvent { delta = delta });

        var hits = _shape.Query();

        var current = new HashSet<object>();
        foreach (var hit in hits) {
            if (hit.Target == null)
                continue;

            var key = (object)hit.Target;
            current.Add(key);

            if (!_inside.Contains(key)) {
                _inside.Add(key);
                HandleEvent(new OnTargetEnterEvent {
                    target = hit.Target,
                    point = hit.Point,
                    normal = hit.Normal
                });
            }

            var hitEvent = new OnHitEvent {
                Target = hit.Target,
                Point = hit.Point,
                Normal = hit.Normal,
                Outcome = HitOutcome.None
            };
            HandleEvent(hitEvent);
        }

        if (_inside.Count > 0) {
            var toRemove = new List<object>();
            foreach (var key in _inside)
                if (!current.Contains(key))
                    toRemove.Add(key);

            foreach (var key in toRemove) {
                _inside.Remove(key);
                HandleEvent(new OnTargetExitEvent { target = (UnityEngine.GameObject)key });
            }
        }
    }

    protected override void OnLifetimeExpired() {
        HandleEnd();
    }

    private void HandleEnd() {
        if (_started) {
            _started = false;
            if (_inside.Count > 0) {
                foreach (var key in _inside)
                    HandleEvent(new OnTargetExitEvent { target = (UnityEngine.GameObject)key });
                _inside.Clear();
            }

            HandleEvent(new OnBeamEndEvent());
        }

        context.View.Kill(context);
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}