using System.Collections.Generic;
using UnityEngine;

public class UnityWorldQuery : IWorldQuery {
    private struct TargetCandidate {
        public GameObject GameObject;
        public ITarget Target;
        public float DistanceSqr;
    }

    private readonly int _enemyMask = 1 << LayerMask.NameToLayer("Player");
    private Collider[] _overlapBuffer = new Collider[64];
    private readonly List<SpellCaster> _nearbyCasters = new();
    private readonly List<SpellInstance> _nearbySpells = new();
    private readonly List<TargetCandidate> _candidates = new();
    private readonly List<ITarget> _results = new();
    private readonly HashSet<GameObject> _seen = new();

    public ITarget FindClosestEnemy(Vector3 pos, float radius) {
        using (SpellMetrics.Measure(SpellMetricSection.WorldQueryFindClosestEnemy)) {
            var count = Physics.OverlapSphereNonAlloc(pos, radius, _overlapBuffer, _enemyMask);
            while (count == _overlapBuffer.Length) {
                _overlapBuffer = new Collider[_overlapBuffer.Length * 2];
                count = Physics.OverlapSphereNonAlloc(pos, radius, _overlapBuffer, _enemyMask);
            }

            ITarget closest = null;
            float best = float.MaxValue;

            for (var i = 0; i < count; i++) {
                var c = _overlapBuffer[i];
                if (c == null) continue;

                var target = c.GetComponent<ITarget>();
                if (target == null) continue;

                float d = (target.Position - pos).sqrMagnitude;
                if (d < best) {
                    best = d;
                    closest = target;
                }
            }

            return closest;
        }
    }

    public bool HasLineOfSight(Vector3 from, Vector3 to) {
        return !Physics.Linecast(from, to, _enemyMask);
    }

    public IEnumerable<ITarget> FindEnemiesInRadius(Vector3 pos, float r) {
        using (SpellMetrics.Measure(SpellMetricSection.WorldQueryFindEnemiesInRadius)) {
            var radiusSqr = r * r;

            _candidates.Clear();
            _results.Clear();
            _seen.Clear();

            WorldTargetIndex.GetCasters(pos, r, _nearbyCasters);
            for (var i = 0; i < _nearbyCasters.Count; i++) {
                var caster = _nearbyCasters[i];
                if (caster == null) continue;
                if (caster.gameObject == null) continue;

                AddCandidate(caster.gameObject, caster, pos, radiusSqr);
            }

            WorldTargetIndex.GetSpells(pos, r, _nearbySpells);
            for (var i = 0; i < _nearbySpells.Count; i++) {
                var spellInstance = _nearbySpells[i];
                if (spellInstance == null) continue;
                if (spellInstance.gameObject == null) continue;
                if (!spellInstance.Bind.Context.View.IsAlive) continue;

                AddCandidate(spellInstance.gameObject, spellInstance, pos, radiusSqr);
            }

            _candidates.Sort((x, y) => x.DistanceSqr.CompareTo(y.DistanceSqr));
            for (var i = 0; i < _candidates.Count; i++) {
                var candidate = _candidates[i];
                if (!_seen.Add(candidate.GameObject))
                    continue;

                _results.Add(candidate.Target);
            }

            return _results;
        }
    }


    private void AddCandidate(GameObject gameObject, ITarget target, Vector3 pos, float radiusSqr) {
        var distanceSqr = (target.Position - pos).sqrMagnitude;
        if (distanceSqr > radiusSqr)
            return;

        _candidates.Add(new TargetCandidate {
            GameObject = gameObject,
            Target = target,
            DistanceSqr = distanceSqr
        });
    }
}