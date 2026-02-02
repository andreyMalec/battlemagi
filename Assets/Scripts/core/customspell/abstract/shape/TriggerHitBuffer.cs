using System.Collections.Generic;
using UnityEngine;

public class TriggerHitBuffer : MonoBehaviour {
    private readonly List<ShapeHit> _hits = new();

    public List<ShapeHit> Consume() {
        if (_hits.Count == 0)
            return _hits;

        var copy = new List<ShapeHit>(_hits);
        _hits.Clear();
        return copy;
    }

    void OnTriggerEnter(Collider other) {
        var point = other.ClosestPoint(transform.position);
        _hits.Add(new ShapeHit {
            Target = other.gameObject,
            Point = point,
            Normal = (transform.position - point).normalized
        });
    }

    void OnTriggerStay(Collider other) {
        var point = other.ClosestPoint(transform.position);
        _hits.Add(new ShapeHit {
            Target = other.gameObject,
            Point = point,
            Normal = (transform.position - point).normalized
        });
    }
}

