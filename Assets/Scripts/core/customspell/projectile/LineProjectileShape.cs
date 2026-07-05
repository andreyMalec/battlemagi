using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LineProjectileShape : IShape {
    private const int MaxHits = 16;

    private ISpellContext _context;
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

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

            var direction = delta / distance;
            var bestDistance = float.MaxValue;

            var hasSolidHit = false;
            RaycastHit hit = default;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _hits,
                distance,
                _context.Spell.defaultRaycast,
                QueryTriggerInteraction.Ignore
            );
            for (var i = 0; i < hitCount; i++) {
                var candidate = _hits[i];
                var collider = candidate.collider;
                if (collider == null)
                    continue;
                if (IsCasterCollider(collider))
                    continue;
                if (candidate.distance >= bestDistance)
                    continue;

                bestDistance = candidate.distance;
                hit = candidate;
                hasSolidHit = true;
            }

            Draggable bestDraggable = null;
            RaycastHit bestDraggableHit = default;
            var ray = new Ray(origin, direction);
            var draggables = Draggable.Active;
            for (var i = 0; i < draggables.Count; i++) {
                var draggable = draggables[i];
                if (draggable == null)
                    continue;
                // check layer of object in raycast layers
                if ((_context.Spell.defaultRaycast & (1 << draggable.gameObject.layer)) == 0)
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

    private bool IsCasterCollider(Collider collider) {
        if (DamageUtils.TryGetOwnerFromCollider(collider, out var dam, out ParticipantId owner) && !dam.IsStructure)
            return owner == _context.OwnerId;

        return false;
    }
}