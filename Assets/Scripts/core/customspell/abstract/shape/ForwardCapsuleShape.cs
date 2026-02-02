using System;
using System.Collections.Generic;
using UnityEngine;

public class ForwardCapsuleShape : MonoBehaviour, IShape {
    private readonly HashSet<GameObject> _inside = new();
    private CapsuleCollider _collider;
    private ISpellContext _context;

    private void Awake() {
        _collider = gameObject.AddComponent<CapsuleCollider>();
        _collider.enabled = false;
        _collider.isTrigger = true;
    }

    public void Init(ISpellContext context) {
        _collider.radius = context.Data.zoneRadius;
        _collider.height = context.Data.projectileSpeed * 2 * context.DeltaTime;
        _collider.direction = 2; // Z axis
        _collider.enabled = true;
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
            if (go != null) {
                yield return new ShapeHit { Target = go, Point = transform.position };
            }
        }
    }
}