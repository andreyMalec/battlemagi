using System.Collections.Generic;
using UnityEngine;

public class BeamCore : ISpellCore<BeamContext> {
    private readonly IShape _shape;
    private readonly HashSet<GameObject> _inside = new();
    private readonly HashSet<GameObject> _current = new();
    private readonly List<GameObject> _exited = new();
    private bool _started;

    public BeamCore(
        BeamContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.BeamCoreTick);
        context.Lifetime -= delta;
        if (!_started) {
            _started = true;
            HandleEvent(new OnBeamStartEvent());
        }

        HandleEvent(new OnBeamTickEvent { delta = delta });

        _current.Clear();
        foreach (var hit in _shape.Query()) {
            if (hit.Target == null)
                continue;

            var key = hit.Target;
            _current.Add(key);

            if (_inside.Add(key)) {
                HandleEvent(new OnTargetEnterEvent {
                    target = hit.Target,
                    point = hit.Point,
                    normal = hit.Normal
                });
            }

            var hitEvent = new OnHitEvent {
                ShapeHit = hit,
                Outcome = HitOutcome.None
            };
            HandleEvent(hitEvent);
        }

        if (_inside.Count > _current.Count) {
            _exited.Clear();
            foreach (var key in _inside) {
                if (_current.Contains(key))
                    continue;

                _exited.Add(key);
            }

            foreach (var key in _exited) {
                _inside.Remove(key);
                HandleEvent(new OnTargetExitEvent { target = key });
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
                    HandleEvent(new OnTargetExitEvent { target = key });
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