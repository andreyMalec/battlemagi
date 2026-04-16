using System;
using System.Collections.Generic;
using UnityEngine;

public class ConeBeamShape : IShape {
    private const int MaxHits = 64;
    private const int MaxLosHits = 32;

    private IBeamContext _ctx;
    private readonly Collider[] _hits = new Collider[MaxHits];
    private readonly RaycastHit[] _losHits = new RaycastHit[MaxLosHits];

    public void Init(ISpellContext context) {
        _ctx = (IBeamContext)context;
    }

    public IEnumerable<ShapeHit> Query() {
        using (SpellMetrics.Measure(SpellMetricSection.ConeBeamShapeQuery)) {
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

                var target = ResolveTarget(collider);
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

                if (!HasLineOfSight(origin, point, target))
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

    private GameObject ResolveTarget(Collider collider) {
        if (DamageUtils.TryGetOwnerFromCollider(collider, out var damageable, out _))
            return damageable.gameObject;

        return collider.gameObject;
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 point, GameObject target) {
        var toPoint = point - origin;
        var distance = toPoint.magnitude;
        if (distance <= 0.01f)
            return true;

        var dir = toPoint / distance;
        var count = Physics.RaycastNonAlloc(
            origin,
            dir,
            _losHits,
            distance,
            _ctx.Spell.defaultRaycast,
            QueryTriggerInteraction.Ignore
        );

        if (count <= 0)
            return true;

        Array.Sort(_losHits, 0, count, RaycastHitDistanceComparer.Instance);

        for (var i = 0; i < count; i++) {
            var hitCollider = _losHits[i].collider;
            if (hitCollider == null)
                continue;

            if (IsCasterCollider(hitCollider))
                continue;

            return ResolveTarget(hitCollider) == target;
        }

        return true;
    }

    private bool IsCasterCollider(Collider collider) {
        return collider.transform.root == _ctx.Caster.transform.root;
    }

    private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit> {
        public static readonly RaycastHitDistanceComparer Instance = new();

        public int Compare(RaycastHit x, RaycastHit y) {
            return x.distance.CompareTo(y.distance);
        }
    }
}
