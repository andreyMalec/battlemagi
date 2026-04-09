using System.Collections.Generic;
using UnityEngine;

public class ConeBeamShape : IShape {
    private const int MaxHits = 64;

    private IBeamContext _ctx;
    private readonly Collider[] _hits = new Collider[MaxHits];

    public void Init(ISpellContext context) {
        _ctx = (IBeamContext)context;
    }

    public IEnumerable<ShapeHit> Query() {
        var dir = _ctx.Direction;
        if (dir.sqrMagnitude <= 0f)
            yield break;

        dir.Normalize();

        var def = _ctx.Spell.beam;
        var origin = _ctx.Origin;
        var startRadius = Mathf.Max(0f, def.coneRadius);
        var length = Mathf.Max(0f, def.coneLength);
        var angle = Mathf.Clamp(def.coneAngle, 0f, 89f);
        var endRadius = startRadius + Mathf.Tan(angle * Mathf.Deg2Rad) * length;

        var count = Physics.OverlapCapsuleNonAlloc(
            origin,
            origin + dir * length,
            Mathf.Max(startRadius, endRadius),
            _hits,
            _ctx.Spell.defaultRaycast,
            QueryTriggerInteraction.Ignore
        );

        var yielded = new HashSet<GameObject>();
        for (var i = 0; i < count; i++) {
            var collider = _hits[i];
            if (collider == null)
                continue;

            var target = collider.gameObject;
            if (!yielded.Add(target))
                continue;

            var center = collider.bounds.center;
            var centerOffset = center - origin;
            var centerDistance = Vector3.Dot(centerOffset, dir);
            var centerOnAxis = origin + dir * Mathf.Clamp(centerDistance, 0f, length);
            var point = collider.ClosestPoint(centerOnAxis);

            var pointOffset = point - origin;
            var distance = Vector3.Dot(pointOffset, dir);
            if (distance < 0f || distance > length)
                continue;

            var axialPoint = origin + dir * distance;
            var radial = point - axialPoint;
            var allowedRadius = startRadius + Mathf.Tan(angle * Mathf.Deg2Rad) * distance;
            if (radial.sqrMagnitude > allowedRadius * allowedRadius)
                continue;

            var normal = radial.sqrMagnitude > 0.0001f ? radial.normalized : -dir;
            yield return new ShapeHit {
                Target = target,
                Point = point,
                Normal = normal
            };
        }
    }
}
