using System.Collections.Generic;
using UnityEngine;

public class TriggerSphereShape : IShape {
    private readonly HashSet<GameObject> _inside = new();
    private readonly List<GameObject> _buffer = new();
    private readonly List<GameObject> _insideCopy = new();
    private readonly Dictionary<GameObject, Vector3> _points = new();

    private ISpellContext _context;
    private float _radius;
    private float _radiusSqr;

    public void Init(ISpellContext context) {
        _context = context;
        _radius = context.Spell.scale;
        _radiusSqr = _radius * _radius;
    }

    public IEnumerable<ShapeHit> Query() {
        var center = _context.Movement.Transform.position;

        _buffer.Clear();
        _points.Clear();
        foreach (var inst in SpellInstance.Active) {
            if (inst == null) continue;
            if (inst.Bind == null) continue;
            if (inst.Bind.Context == _context) continue;
            if (!inst.Bind.Context.View.IsAlive) continue;

            var targetGo = inst.Bind.Context.View.gameObject;
            var d = targetGo.transform.position - center;
            if (d.sqrMagnitude > _radiusSqr) continue;

            _buffer.Add(targetGo);
            _points[targetGo] = targetGo.transform.position;
            if (_inside.Add(targetGo)) {
                Debug.Log($"[{_context.View.name}] OnZoneEnterEvent {targetGo}");
                _context.SendEvent(new OnZoneEnterEvent(targetGo));
            }
        }

        foreach (var inst in Damageable.Active) {
            if (inst == null) continue;
            if (inst.IsDead) continue;

            var targetGo = inst.gameObject;
            var point = targetGo.transform.position;
            if (inst.IsStructure) {
                var zoneCollider = inst.Collider;
                if (zoneCollider.type is DamageableColliderType.Box) {
                    var localPoint = zoneCollider.transform.InverseTransformPoint(center) - zoneCollider.center;
                    var clampedLocalPoint = new Vector3(
                        Mathf.Clamp(localPoint.x, -zoneCollider.halfExtents.x, zoneCollider.halfExtents.x),
                        Mathf.Clamp(localPoint.y, -zoneCollider.halfExtents.y, zoneCollider.halfExtents.y),
                        Mathf.Clamp(localPoint.z, -zoneCollider.halfExtents.z, zoneCollider.halfExtents.z)
                    );
                    point = zoneCollider.transform.TransformPoint(clampedLocalPoint + zoneCollider.center);
                }
            }

            var d = point - center;
            if (d.sqrMagnitude > _radiusSqr) continue;

            _buffer.Add(targetGo);
            _points[targetGo] = point;
            if (_inside.Add(targetGo))
                _context.SendEvent(new OnZoneEnterEvent(targetGo));
        }

        if (_inside.Count != _buffer.Count) {
            _insideCopy.Clear();
            foreach (var go in _inside)
                _insideCopy.Add(go);

            foreach (var go in _insideCopy) {
                if (_buffer.Contains(go)) continue;
                _inside.Remove(go);
                _points.Remove(go);
                Debug.Log($"[{_context.View.name}] OnZoneExitEvent {go}");
                _context.SendEvent(new OnZoneExitEvent(go));
            }
        }

        foreach (var go in _inside) {
            if (go == null) continue;
            yield return new ShapeHit {
                Target = go,
                Point = _points.TryGetValue(go, out var point) ? point : go.transform.position
            };
        }

#if UNITY_EDITOR
        Debug.DrawRay(center, Vector3.down * _radius, Color.red);
        Debug.DrawRay(center, Vector3.up * _radius, Color.red);
        Debug.DrawRay(center, Vector3.left * _radius, Color.red);
        Debug.DrawRay(center, Vector3.right * _radius, Color.red);
        Debug.DrawRay(center, Vector3.forward * _radius, Color.red);
        Debug.DrawRay(center, Vector3.back * _radius, Color.red);
#endif
    }
}