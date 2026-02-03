using System;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleProjectileShape : MonoBehaviour, IShape {
    private ISpellContext _context;

    public void Init(ISpellContext context) {
        _context = context;
    }

    public IEnumerable<ShapeHit> Query() {
        var newPos = _context.Movement.Sample(_context.DeltaTime);
        if (Physics.Linecast(transform.position, newPos, out var hit)) {
            yield return new ShapeHit { Target = hit.collider.gameObject, Point = hit.point, Normal = hit.normal };
        }
    }
}