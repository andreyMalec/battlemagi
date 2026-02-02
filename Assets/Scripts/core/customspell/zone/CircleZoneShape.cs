using System.Collections.Generic;
using UnityEngine;

public class CircleZoneShape : IShape<IZoneContext, ZoneVolume> {
    private readonly float _radius;
    private Collider[] _hits;
    private int _playerMask = LayerMask.NameToLayer("Player");

    public CircleZoneShape(float radius) {
        _radius = radius;
        _hits = new Collider[10];
    }

    public ZoneVolume Sample(IZoneContext ctx) {
        return new ZoneVolume {
            Volume = new SphereVolume {
                Center = ctx.Center,
                Radius = _radius
            }
        };
    }

    public IEnumerable<ShapeHit> Query(IZoneContext ctx, ZoneVolume result) {
        if (ctx.View.TryGetComponent<TriggerHitBuffer>(out var buffer))
            return buffer.Consume();

        var size = Physics.OverlapSphereNonAlloc(ctx.Center, _radius, _hits, 1 << _playerMask);

        var shapeHits = new List<ShapeHit>();
        for (var i = 0; i < size; i++) {
            var col = _hits[i];

            shapeHits.Add(new ShapeHit {
                Target = col.gameObject,
                Point = col.transform.position,
            });
        }

        return shapeHits;
    }
}