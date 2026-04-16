using System.Collections.Generic;
using UnityEngine;

public class TriggerSphereShape : IShape {
    private readonly HashSet<GameObject> _inside = new();
    private readonly HashSet<GameObject> _current = new();
    private readonly List<GameObject> _exited = new();
    private readonly Dictionary<GameObject, Vector3> _points = new();
    private readonly List<SpellInstance> _spellCandidates = new();
    private readonly List<Damageable> _damageableCandidates = new();

    private ISpellContext _context;
    private float _radius;
    private float _radiusSqr;

    public void Init(ISpellContext context) {
        _context = context;
        _radius = context.Spell.scale;
        _radiusSqr = _radius * _radius;
    }

    public IEnumerable<ShapeHit> Query() {
        using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereQuery)) {
            var center = _context.Movement.Transform.position;
            var logsEnabled = GameConfig.SpellDebugLogsEnabled;

            _current.Clear();
            _points.Clear();
            using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereSpellScan)) {
                WorldTargetIndex.GetSpells(center, _radius, _spellCandidates);
                for (var i = 0; i < _spellCandidates.Count; i++) {
                    var inst = _spellCandidates[i];
                    if (inst == null) continue;
                    if (inst.Bind == null) continue;
                    if (inst.Bind.Context == _context) continue;
                    if (!inst.Bind.Context.View.IsAlive) continue;

                    var targetGo = inst.Bind.Context.View.gameObject;
                    var d = targetGo.transform.position - center;
                    if (d.sqrMagnitude > _radiusSqr) continue;

                    _current.Add(targetGo);
                    _points[targetGo] = targetGo.transform.position;
                    if (_inside.Add(targetGo)) {
                        if (logsEnabled)
                            SpellLog.Log($"[{_context.View.name}] OnZoneEnterEvent {targetGo}");
                        _context.SendEvent(new OnZoneEnterEvent(targetGo));
                    }
                }
            }

            using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereDamageableScan)) {
                WorldTargetIndex.GetDamageables(center, _radius, _damageableCandidates);
                for (var i = 0; i < _damageableCandidates.Count; i++) {
                    var inst = _damageableCandidates[i];
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

                    _current.Add(targetGo);
                    _points[targetGo] = point;
                    if (_inside.Add(targetGo))
                        _context.SendEvent(new OnZoneEnterEvent(targetGo));
                }
            }

            if (_inside.Count != _current.Count) {
                _exited.Clear();
                foreach (var go in _inside) {
                    if (_current.Contains(go)) continue;
                    _exited.Add(go);
                }

                foreach (var go in _exited) {
                    _inside.Remove(go);
                    _points.Remove(go);
                    if (logsEnabled)
                        SpellLog.Log($"[{_context.View.name}] OnZoneExitEvent {go}");
                    _context.SendEvent(new OnZoneExitEvent(go));
                }
            }

            using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereYieldHits)) {
                foreach (var go in _inside) {
                    if (go == null) continue;
                    yield return new ShapeHit {
                        Target = go,
                        Point = _points.TryGetValue(go, out var point) ? point : go.transform.position
                    };
                }
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
}