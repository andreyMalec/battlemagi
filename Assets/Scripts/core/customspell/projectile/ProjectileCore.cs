using System.Collections.Generic;
using UnityEngine;

public class ProjectileCore : ISpellCore<ProjectileContext> {
    private const int MaxHitscanInteractionsPerTick = 32;
    private const float PierceForwardOffset = 0.05f;
    private bool hitscanResolved;

    private readonly IShape _shape;

    public ProjectileCore(
        ProjectileContext ctx,
        IShape shape,
        SpellTrigger[] triggers
    ) : base(ctx, triggers) {
        _shape = shape;
    }

    protected override void TickInner(float delta) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.ProjectileCoreTick);
        context.Lifetime -= delta;

        if (context.Spell.projectile.moveType == SpellMovement.Hitscan) {
            if (!hitscanResolved)
                ResolveHitscanInteractions();
            hitscanResolved = true;
            return;
        }

        var hits = _shape.Query();

        foreach (var hit in hits) {
            var hitEvent = new OnHitEvent {
                ShapeHit = hit,
                Outcome = HitOutcome.Destroy
            };

            HandleEvent(hitEvent);
            if ((hitEvent.Outcome & HitOutcome.Destroy) != 0) {
                context.View.Kill(context);
                return;
            }
        }
    }

    private void ResolveHitscanInteractions() {
        List<Vector3> hits = new();
        hits.Add(context.Movement.Transform.position);
        for (var i = 0; i < MaxHitscanInteractionsPerTick; i++) {
            if (!TryGetFirstHit(out var hit))
                break;

            hits.Add(hit.Point);
            var hitEvent = new OnHitEvent {
                ShapeHit = hit,
                Outcome = HitOutcome.Destroy
            };

            HandleEvent(hitEvent);
            if ((hitEvent.Outcome & HitOutcome.Destroy) != 0) {
                context.View.Kill(context);
                break;
            }

            if ((hitEvent.Outcome & HitOutcome.Pierce) != 0) {
                var direction = context.Movement.Motion.Velocity;
                if (direction.sqrMagnitude <= 0f)
                    direction = context.Movement.Transform.forward;
                context.Movement.Transform.position =
                    hitEvent.ShapeHit.Point + direction.normalized * PierceForwardOffset;
                continue;
            }

            if ((hitEvent.Outcome & HitOutcome.Bounce) != 0)
                continue;

            break;
        }

        hits.Add(context.Movement.Sample(0));
        context.Event.OnTrajectoryConfirmed(context.View, hits);
    }

    private bool TryGetFirstHit(out ShapeHit hit) {
        foreach (var shapeHit in _shape.Query()) {
            hit = shapeHit;
            return true;
        }

        hit = default;
        return false;
    }

    protected override void AttachEventSink() {
        context.eventSink = HandleEvent;
    }
}