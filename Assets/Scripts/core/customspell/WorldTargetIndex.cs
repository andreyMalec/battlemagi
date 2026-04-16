using System;
using System.Collections.Generic;
using UnityEngine;

public static class WorldTargetIndex {
    private const float CellSize = 8f;

    private struct CellKey : IEquatable<CellKey> {
        public int X;
        public int Y;
        public int Z;

        public bool Equals(CellKey other) {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj) {
            return obj is CellKey other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                var hash = X;
                hash = hash * 397 ^ Y;
                hash = hash * 397 ^ Z;
                return hash;
            }
        }
    }

    private sealed class Bucket {
        public readonly List<SpellInstance> Spells = new();
        public readonly List<Damageable> Damageables = new();
        public readonly List<SpellCaster> Casters = new();
        public int BuildVersion;
    }

    private static readonly Dictionary<CellKey, Bucket> Buckets = new();
    private static readonly List<Bucket> UsedBuckets = new();
    private static readonly HashSet<SpellInstance> SeenSpells = new();
    private static readonly HashSet<Damageable> SeenDamageables = new();
    private static readonly HashSet<SpellCaster> SeenCasters = new();

    private static int _buildVersion;
    private static int _lastFrameCount = -1;
    private static int _lastFixedStep = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        Buckets.Clear();
        UsedBuckets.Clear();
        SeenSpells.Clear();
        SeenDamageables.Clear();
        SeenCasters.Clear();
        _buildVersion = 0;
        _lastFrameCount = -1;
        _lastFixedStep = int.MinValue;
    }

    public static void GetSpells(Vector3 center, float radius, List<SpellInstance> results) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.WorldTargetIndexQuerySpells);
        EnsureBuilt();
        results.Clear();
        SeenSpells.Clear();
        var min = WorldToCell(center - Vector3.one * radius);
        var max = WorldToCell(center + Vector3.one * radius);
        for (var x = min.X; x <= max.X; x++) {
            for (var y = min.Y; y <= max.Y; y++) {
                for (var z = min.Z; z <= max.Z; z++) {
                    if (!Buckets.TryGetValue(new CellKey { X = x, Y = y, Z = z }, out var bucket))
                        continue;

                    for (var i = 0; i < bucket.Spells.Count; i++) {
                        var spell = bucket.Spells[i];
                        if (!SeenSpells.Add(spell))
                            continue;
                        results.Add(spell);
                    }
                }
            }
        }
    }

    public static void GetDamageables(Vector3 center, float radius, List<Damageable> results) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.WorldTargetIndexQueryDamageables);
        EnsureBuilt();
        results.Clear();
        SeenDamageables.Clear();
        var min = WorldToCell(center - Vector3.one * radius);
        var max = WorldToCell(center + Vector3.one * radius);
        for (var x = min.X; x <= max.X; x++) {
            for (var y = min.Y; y <= max.Y; y++) {
                for (var z = min.Z; z <= max.Z; z++) {
                    if (!Buckets.TryGetValue(new CellKey { X = x, Y = y, Z = z }, out var bucket))
                        continue;

                    for (var i = 0; i < bucket.Damageables.Count; i++) {
                        var damageable = bucket.Damageables[i];
                        if (!SeenDamageables.Add(damageable))
                            continue;
                        results.Add(damageable);
                    }
                }
            }
        }
    }

    public static void GetCasters(Vector3 center, float radius, List<SpellCaster> results) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.WorldTargetIndexQueryCasters);
        EnsureBuilt();
        results.Clear();
        SeenCasters.Clear();
        var min = WorldToCell(center - Vector3.one * radius);
        var max = WorldToCell(center + Vector3.one * radius);
        for (var x = min.X; x <= max.X; x++) {
            for (var y = min.Y; y <= max.Y; y++) {
                for (var z = min.Z; z <= max.Z; z++) {
                    if (!Buckets.TryGetValue(new CellKey { X = x, Y = y, Z = z }, out var bucket))
                        continue;

                    for (var i = 0; i < bucket.Casters.Count; i++) {
                        var caster = bucket.Casters[i];
                        if (!SeenCasters.Add(caster))
                            continue;
                        results.Add(caster);
                    }
                }
            }
        }
    }

    private static void EnsureBuilt() {
        if (!NeedsRebuild())
            return;

        using var _ = SpellMetrics.Measure(SpellMetricSection.WorldTargetIndexRebuild);
        _buildVersion++;
        ClearUsedBuckets();
        BuildSpells();
        BuildDamageables();
        BuildCasters();
    }

    private static bool NeedsRebuild() {
        if (!Application.isPlaying)
            return true;

        if (Time.inFixedTimeStep) {
            var fixedStep = Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime);
            if (_lastFixedStep == fixedStep)
                return false;

            _lastFixedStep = fixedStep;
            _lastFrameCount = Time.frameCount;
            return true;
        }

        if (_lastFrameCount == Time.frameCount)
            return false;

        _lastFrameCount = Time.frameCount;
        _lastFixedStep = int.MinValue;
        return true;
    }

    private static void ClearUsedBuckets() {
        for (var i = 0; i < UsedBuckets.Count; i++) {
            var bucket = UsedBuckets[i];
            bucket.Spells.Clear();
            bucket.Damageables.Clear();
            bucket.Casters.Clear();
        }

        UsedBuckets.Clear();
    }

    private static void BuildSpells() {
        for (var i = 0; i < SpellInstance.Active.Count; i++) {
            var spell = SpellInstance.Active[i];
            if (spell == null) continue;
            if (spell.Bind == null) continue;
            if (!spell.Bind.Context.View.IsAlive) continue;

            AddSpell(spell.Bind.Context.View.transform.position, spell);
        }
    }

    private static void BuildDamageables() {
        for (var i = 0; i < Damageable.Active.Count; i++) {
            var damageable = Damageable.Active[i];
            if (damageable == null) continue;
            if (damageable.IsDead) continue;

            if (damageable.IsStructure && damageable.Collider.type is DamageableColliderType.Box) {
                AddDamageableBounds(damageable, damageable.Collider);
                continue;
            }

            AddDamageable(damageable.transform.position, damageable);
        }
    }

    private static void BuildCasters() {
        for (var i = 0; i < SpellCaster.Active.Count; i++) {
            var caster = SpellCaster.Active[i];
            if (caster == null) continue;
            AddCaster(caster.Position, caster);
        }
    }

    private static void AddSpell(Vector3 position, SpellInstance spell) {
        var bucket = GetBucket(WorldToCell(position));
        bucket.Spells.Add(spell);
    }

    private static void AddDamageable(Vector3 position, Damageable damageable) {
        var bucket = GetBucket(WorldToCell(position));
        bucket.Damageables.Add(damageable);
    }

    private static void AddCaster(Vector3 position, SpellCaster caster) {
        var bucket = GetBucket(WorldToCell(position));
        bucket.Casters.Add(caster);
    }

    private static void AddDamageableBounds(Damageable damageable, DamageableCollider collider) {
        var center = collider.transform.TransformPoint(collider.center);
        var extents = GetWorldExtents(collider.transform, collider.halfExtents);
        var min = WorldToCell(center - extents);
        var max = WorldToCell(center + extents);
        for (var x = min.X; x <= max.X; x++) {
            for (var y = min.Y; y <= max.Y; y++) {
                for (var z = min.Z; z <= max.Z; z++) {
                    var bucket = GetBucket(new CellKey { X = x, Y = y, Z = z });
                    bucket.Damageables.Add(damageable);
                }
            }
        }
    }

    private static Vector3 GetWorldExtents(Transform transform, Vector3 localHalfExtents) {
        var x = transform.TransformVector(new Vector3(localHalfExtents.x, 0f, 0f));
        var y = transform.TransformVector(new Vector3(0f, localHalfExtents.y, 0f));
        var z = transform.TransformVector(new Vector3(0f, 0f, localHalfExtents.z));
        return new Vector3(
            Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
            Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)
        );
    }

    private static Bucket GetBucket(CellKey key) {
        if (!Buckets.TryGetValue(key, out var bucket)) {
            bucket = new Bucket();
            Buckets.Add(key, bucket);
        }

        if (bucket.BuildVersion != _buildVersion) {
            bucket.BuildVersion = _buildVersion;
            UsedBuckets.Add(bucket);
        }

        return bucket;
    }

    private static CellKey WorldToCell(Vector3 position) {
        return new CellKey {
            X = Mathf.FloorToInt(position.x / CellSize),
            Y = Mathf.FloorToInt(position.y / CellSize),
            Z = Mathf.FloorToInt(position.z / CellSize)
        };
    }
}


