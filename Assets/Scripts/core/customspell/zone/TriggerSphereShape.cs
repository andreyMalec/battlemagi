using System;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSphereShape : IShape {
    private readonly HashSet<GameObject> _inside = new();
    private ISpellContext _context;

    public void Init(ISpellContext context) {
    }

    void OnTriggerEnter(Collider other) {
        if (other.isTrigger) return;
        _inside.Add(other.gameObject);
    }

    void OnTriggerExit(Collider other) {
        _inside.Remove(other.gameObject);
    }

    public IEnumerable<ShapeHit> Query() {
        foreach (var go in _inside) {
            if (go != null)
                yield return new ShapeHit { Target = go, Point = go.transform.position };
        }
    }
}