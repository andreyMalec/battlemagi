using System;
using System.Collections.Generic;
using UnityEngine;

public class LineProjectileShape : IShape {
    private ISpellContext _context;

    public void Init(ISpellContext context) {
        _context = context;
    }

    public IEnumerable<ShapeHit> Query() {
        using (SpellMetrics.Measure(SpellMetricSection.LineProjectileShapeQuery)) {
            var newPos = _context.Movement.Sample(_context.DeltaTime);
            if (Physics.Linecast(_context.Movement.Transform.position, newPos, out var hit, _context.Spell.defaultRaycast,
                    QueryTriggerInteraction.Ignore)) {
                yield return new ShapeHit { Target = hit.collider.gameObject, Point = hit.point, Normal = hit.normal };
            }
        }
    }
}