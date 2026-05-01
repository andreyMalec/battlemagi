using System;
using System.Text;
using Unity.Profiling;
using UnityEngine;

public enum SpellMetricSection {
    TickerFixedUpdate,
    ActiveTick,
    InstanceTick,
    BindTick,
    SummonBindTick,
    CoreTick,
    TransformTick,
    SummonSensorsTick,
    SummonBrainTick,
    SummonCommandsTick,
    EventDispatch,
    BeamCoreTick,
    ProjectileCoreTick,
    ZoneCoreTick,
    TriggerSphereQuery,
    TriggerSphereSpellScan,
    TriggerSphereDamageableScan,
    TriggerSphereYieldHits,
    StraightBeamShapeQuery,
    ConeBeamShapeQuery,
    LineProjectileShapeQuery,
    WorldQueryFindClosestEnemy,
    WorldQueryFindEnemiesInRadius,
    WorldQueryCaptureCasters,
    WorldQueryCaptureSpells,
    WorldTargetIndexRebuild,
    WorldTargetIndexQuerySpells,
    WorldTargetIndexQueryDamageables,
    WorldTargetIndexQueryCasters,
    HomingTick,
    HomingAcquireTarget,
    HomingLineOfSight,
    HomingObstacleAvoidance
}

public static class SpellMetrics {
    private struct Stat {
        public int Calls;
        public double TotalMs;
        public float MaxMs;
    }

    private static readonly string[] Names = {
        "Spell/TickerFixedUpdate",
        "Spell/ActiveTick",
        "Spell/InstanceTick",
        "Spell/BindTick",
        "Spell/SummonBindTick",
        "Spell/CoreTick",
        "Spell/TransformTick",
        "Spell/SummonSensorsTick",
        "Spell/SummonBrainTick",
        "Spell/SummonCommandsTick",
        "Spell/EventDispatch",
        "Spell/BeamCoreTick",
        "Spell/ProjectileCoreTick",
        "Spell/ZoneCoreTick",
        "Spell/TriggerSphereQuery",
        "Spell/TriggerSphereSpellScan",
        "Spell/TriggerSphereDamageableScan",
        "Spell/TriggerSphereYieldHits",
        "Spell/StraightBeamShapeQuery",
        "Spell/ConeBeamShapeQuery",
        "Spell/LineProjectileShapeQuery",
        "Spell/WorldQueryFindClosestEnemy",
        "Spell/WorldQueryFindEnemiesInRadius",
        "Spell/WorldQueryCaptureCasters",
        "Spell/WorldQueryCaptureSpells",
        "Spell/WorldTargetIndexRebuild",
        "Spell/WorldTargetIndexQuerySpells",
        "Spell/WorldTargetIndexQueryDamageables",
        "Spell/WorldTargetIndexQueryCasters",
        "Spell/HomingTick",
        "Spell/HomingAcquireTarget",
        "Spell/HomingLineOfSight",
        "Spell/HomingObstacleAvoidance"
    };

    private static readonly ProfilerMarker[] Markers = CreateMarkers();
    private static readonly Stat[] Stats = new Stat[Names.Length];

    private static float _nextSummaryAt;
    private static int _maxActiveSpells;

    public static Scope Measure(SpellMetricSection section) {
        return new Scope(section);
    }

    public static void RecordActiveSpells(int count) {
        if (!GameConfig.SpellMetricsSummaryLogsEnabled)
            return;

        if (count > _maxActiveSpells)
            _maxActiveSpells = count;
    }

    public static void FlushIfNeeded() {
        if (!GameConfig.SpellMetricsSummaryLogsEnabled)
            return;

        var now = Time.realtimeSinceStartup;
        var interval = Mathf.Max(0.5f, GameConfig.SpellMetricsSummaryInterval);
        if (_nextSummaryAt <= 0f) {
            _nextSummaryAt = now + interval;
            return;
        }

        if (now < _nextSummaryAt)
            return;

        _nextSummaryAt = now + interval;
        LogAndReset(interval);
    }

    private static ProfilerMarker[] CreateMarkers() {
        var markers = new ProfilerMarker[Names.Length];
        for (var i = 0; i < markers.Length; i++)
            markers[i] = new ProfilerMarker(Names[i]);
        return markers;
    }

    private static void AddSample(SpellMetricSection section, float elapsedMs) {
        var index = (int)section;
        var stat = Stats[index];
        stat.Calls++;
        stat.TotalMs += elapsedMs;
        if (elapsedMs > stat.MaxMs)
            stat.MaxMs = elapsedMs;
        Stats[index] = stat;
    }

    private static void LogAndReset(float interval) {
        var lines = 0;
        for (var i = 0; i < Stats.Length; i++) {
            if (Stats[i].Calls > 0)
                lines++;
        }

        if (lines == 0) {
            _maxActiveSpells = 0;
            return;
        }

        var used = new bool[Stats.Length];
        var builder = new StringBuilder(512);
        builder.Append("[SpellMetrics] ")
            .Append(interval.ToString("0.0"))
            .Append("s window")
            .Append(", maxActive=")
            .Append(_maxActiveSpells);

        var topCount = Mathf.Min(6, lines);
        for (var rank = 0; rank < topCount; rank++) {
            var bestIndex = -1;
            var bestTotalMs = 0d;
            for (var i = 0; i < Stats.Length; i++) {
                if (used[i])
                    continue;
                if (Stats[i].Calls == 0)
                    continue;
                if (Stats[i].TotalMs <= bestTotalMs)
                    continue;

                bestIndex = i;
                bestTotalMs = Stats[i].TotalMs;
            }

            if (bestIndex < 0)
                break;

            used[bestIndex] = true;
            var stat = Stats[bestIndex];
            var avgMs = stat.TotalMs / stat.Calls;
            builder.Append('\n')
                .Append(rank + 1)
                .Append(". ")
                .Append(Names[bestIndex])
                .Append(": total=")
                .Append(stat.TotalMs.ToString("0.###"))
                .Append("ms avg=")
                .Append(avgMs.ToString("0.###"))
                .Append("ms max=")
                .Append(stat.MaxMs.ToString("0.###"))
                .Append("ms calls=")
                .Append(stat.Calls);
        }

        Debug.Log(builder.ToString());

        for (var i = 0; i < Stats.Length; i++)
            Stats[i] = default;

        _maxActiveSpells = 0;
    }

    public readonly struct Scope : IDisposable {
        private readonly SpellMetricSection _section;
        private readonly bool _profilerEnabled;
        private readonly bool _summaryEnabled;
        private readonly float _startTime;

        public Scope(SpellMetricSection section) {
            _section = section;
            _profilerEnabled = GameConfig.SpellMetricsEnabled;
            _summaryEnabled = GameConfig.SpellMetricsSummaryLogsEnabled;
            if (_profilerEnabled)
                Markers[(int)section].Begin();
            _startTime = _summaryEnabled ? Time.realtimeSinceStartup : 0f;
        }

        public void Dispose() {
            if (_profilerEnabled)
                Markers[(int)_section].End();
            if (_summaryEnabled)
                AddSample(_section, (Time.realtimeSinceStartup - _startTime) * 1000f);
        }
    }
}


