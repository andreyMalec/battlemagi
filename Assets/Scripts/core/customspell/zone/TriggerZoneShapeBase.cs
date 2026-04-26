using System.Collections.Generic;
using UnityEngine;

public abstract class TriggerZoneShapeBase : IShape {
    private readonly HashSet<GameObject> _inside = new();
    private readonly HashSet<GameObject> _current = new();
    private readonly List<GameObject> _exited = new();
    private readonly Dictionary<GameObject, Vector3> _points = new();
    private readonly List<SpellInstance> _spellCandidates = new();
    private readonly List<Damageable> _damageableCandidates = new();

    protected ISpellContext Context { get; private set; }

    public void Init(ISpellContext context) {
        Context = context;
        InitShape(context);
    }

    protected virtual void InitShape(ISpellContext context) {
    }

    public IEnumerable<ShapeHit> Query() {
        using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereQuery)) {
            var center = Context.Movement.Transform.position;
            var bounds = GetBroadphaseBounds(center);
            var logsEnabled = GameConfig.SpellDebugLogsEnabled;

            _current.Clear();
            _points.Clear();
            using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereSpellScan)) {
                WorldTargetIndex.GetSpellsInBounds(bounds.min, bounds.max, _spellCandidates);
                for (var i = 0; i < _spellCandidates.Count; i++) {
                    var inst = _spellCandidates[i];
                    if (inst == null) continue;
                    if (inst.Bind == null) continue;
                    if (inst.Bind.Context == Context) continue;
                    if (!inst.Bind.Context.View.IsAlive) continue;

                    var targetGo = inst.Bind.Context.View.gameObject;
                    var point = targetGo.transform.position;
                    if (!ContainsPoint(point)) continue;

                    _current.Add(targetGo);
                    _points[targetGo] = point;
                    if (_inside.Add(targetGo)) {
                        if (logsEnabled)
                            SpellLog.Log($"[{Context.View.name}] OnZoneEnterEvent {targetGo}");
                        Context.SendEvent(new OnZoneEnterEvent(targetGo));
                    }
                }
            }

            using (SpellMetrics.Measure(SpellMetricSection.TriggerSphereDamageableScan)) {
                WorldTargetIndex.GetDamageablesInBounds(bounds.min, bounds.max, _damageableCandidates);
                for (var i = 0; i < _damageableCandidates.Count; i++) {
                    var inst = _damageableCandidates[i];
                    if (inst == null) continue;
                    if (inst.IsDead) continue;

                    var targetGo = inst.gameObject;
                    if (!TryGetDamageablePoint(inst, out var point)) continue;

                    _current.Add(targetGo);
                    _points[targetGo] = point;
                    if (_inside.Add(targetGo))
                        Context.SendEvent(new OnZoneEnterEvent(targetGo));
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
                        SpellLog.Log($"[{Context.View.name}] OnZoneExitEvent {go}");
                    Context.SendEvent(new OnZoneExitEvent(go));
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
            DrawDebug(center);
#endif
        }
    }

    protected abstract Bounds GetBroadphaseBounds(Vector3 center);

    protected abstract bool ContainsPoint(Vector3 point);

    protected abstract bool TryGetStructurePoint(DamageableCollider collider, out Vector3 point);

    protected virtual void DrawDebug(Vector3 center) {
    }

    private bool TryGetDamageablePoint(Damageable damageable, out Vector3 point) {
        point = damageable.transform.position;
        if (damageable.IsStructure && damageable.Collider.type is DamageableColliderType.Box)
            return TryGetStructurePoint(damageable.Collider, out point);
        return ContainsPoint(point);
    }
}
