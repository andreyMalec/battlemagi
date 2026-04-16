using System.Collections.Generic;
using UnityEngine;

public class StraightBeamShape : IShape {
    private const int MaxHits = 32;

    private IBeamContext _ctx;
    private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

    public void Init(ISpellContext context) {
        _ctx = (IBeamContext)context;
    }

    public IEnumerable<ShapeHit> Query() {
        using (SpellMetrics.Measure(SpellMetricSection.StraightBeamShapeQuery)) {
            var ray = new Ray(_ctx.Origin, _ctx.Direction);
            var count = Physics.RaycastNonAlloc(ray, _hits, _ctx.MaxLength, _ctx.Spell.defaultRaycast,
                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++) {
                var hit = _hits[i];
                yield return new ShapeHit {
                    Target = hit.collider.gameObject,
                    Point = hit.point,
                    Normal = hit.normal
                };
            }
        }
    }
}