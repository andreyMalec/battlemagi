using System.Collections.Generic;
using UnityEngine;

public class LineProjectileShape : IShape {
    private ISpellContext _context;

    public void Init(ISpellContext context) {
        _context = context;
    }

    public IEnumerable<ShapeHit> Query() {
        using (SpellMetrics.Measure(SpellMetricSection.LineProjectileShapeQuery)) {
            var origin = _context.Movement.Transform.position;
            var newPos = _context.Movement.Sample(_context.DeltaTime);
            var delta = newPos - origin;
            var distance = delta.magnitude;
            if (distance <= 0.0001f)
                yield break;

            var bestDistance = float.MaxValue;
            var hasSolidHit = Physics.Linecast(origin, newPos, out var hit, _context.Spell.defaultRaycast, QueryTriggerInteraction.Ignore);
            if (hasSolidHit)
                bestDistance = Vector3.Distance(origin, hit.point);

            Draggable bestDraggable = null;
            RaycastHit bestDraggableHit = default;
            var ray = new Ray(origin, delta / distance);
            var draggables = Draggable.Active;
            for (var i = 0; i < draggables.Count; i++) {
                var draggable = draggables[i];
                if (draggable == null)
                    continue;
                if (!draggable.TryRaycast(ray, distance, out var draggableHit))
                    continue;
                if (draggableHit.distance >= bestDistance)
                    continue;

                bestDistance = draggableHit.distance;
                bestDraggable = draggable;
                bestDraggableHit = draggableHit;
            }

            if (bestDraggable != null) {
                yield return new ShapeHit {
                    Target = bestDraggable.gameObject,
                    Point = bestDraggableHit.point,
                    Normal = bestDraggableHit.normal
                };
                yield break;
            }

            if (hasSolidHit) {
                yield return new ShapeHit { Target = hit.collider.gameObject, Point = hit.point, Normal = hit.normal };
            }
        }
    }
}