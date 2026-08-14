using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DelayedDamageModule : IDamageModule, IDamageModifier {
    private struct DelayedDamageEntry {
        public string source;
        public ParticipantId fromId;
        public DamageKind kind;
        public float remainingDamage;
        public float remainingTime;
    }

    [SerializeField] private float damageTick = 0.5f;

    private readonly List<DelayedDamageEntry> _delayedDamage = new();
    private readonly List<DelayedDamageEntry> _delayedTickBuffer = new();
    private float _tickAccumulator;
    private bool _damageDelayActive;
    private float _damageDelayPercent;
    private float _damageDelayDuration;

    private Damageable _damageable;

    public void Initialize(Damageable damageable, Stats stats) {
        _damageable = damageable;
    }

    public void SetDamageDelayConfig(float delayedPercent, float delayDuration) {
        _damageDelayActive = true;
        _damageDelayPercent = Mathf.Clamp01(delayedPercent);
        _damageDelayDuration = Mathf.Max(0.01f, delayDuration);
    }

    public void ClearDamageDelayConfig() {
        _damageDelayActive = false;
    }

    public void TickServer(float dt) {
        if (_delayedDamage.Count == 0) return;
        if (dt <= 0f) return;

        var tickInterval = damageTick > 0f ? damageTick : 0.1f;
        _tickAccumulator += dt;
        if (_tickAccumulator < tickInterval)
            return;

        var tickCount = Mathf.FloorToInt(_tickAccumulator / tickInterval);
        _tickAccumulator -= tickCount * tickInterval;

        _delayedTickBuffer.Clear();
        for (var step = 0; step < tickCount; step++) {
            for (var i = _delayedDamage.Count - 1; i >= 0; i--) {
                var entry = _delayedDamage[i];
                if (entry.remainingDamage <= 0f) {
                    _delayedDamage.RemoveAt(i);
                    continue;
                }

                var elapsed = Mathf.Min(tickInterval, entry.remainingTime);
                var tickAmount = entry.remainingTime <= elapsed
                    ? entry.remainingDamage
                    : entry.remainingDamage * (elapsed / entry.remainingTime);

                tickAmount = Mathf.Min(tickAmount, entry.remainingDamage);
                entry.remainingDamage -= tickAmount;
                entry.remainingTime = Mathf.Max(0f, entry.remainingTime - elapsed);

                if (entry.remainingDamage <= 0.1f || entry.remainingTime <= 0f)
                    _delayedDamage.RemoveAt(i);
                else
                    _delayedDamage[i] = entry;

                if (tickAmount > 0f) {
                    _delayedTickBuffer.Add(new DelayedDamageEntry {
                        source = entry.source,
                        fromId = entry.fromId,
                        kind = entry.kind,
                        remainingDamage = tickAmount,
                        remainingTime = 0f
                    });
                }
            }
        }

        for (var i = 0; i < _delayedTickBuffer.Count; i++) {
            var tick = _delayedTickBuffer[i];
            _damageable.TakeDamage(
                tick.source,
                tick.fromId,
                tick.remainingDamage,
                tick.kind
            );
        }
    }

    private bool TryDelayIncomingDamage(ref DamageRequest request, float currentAmount) {
        if (!_damageDelayActive) return false;
        if (_damageDelayPercent <= 0f) return false;
        if (_damageDelayDuration <= 0f) return false;
        if (currentAmount <= 0f) return false;

        var delayedAmount = currentAmount * _damageDelayPercent;
        var immediateAmount = Mathf.Max(0f, currentAmount - delayedAmount);
        if (delayedAmount > 0f) {
            _delayedDamage.Add(new DelayedDamageEntry {
                source = request.source,
                fromId = request.fromId,
                kind = DamageKind.Delayed,
                remainingDamage = delayedAmount,
                remainingTime = _damageDelayDuration
            });
        }

        request = new DamageRequest(request.source, request.fromId, immediateAmount, DamageKind.Delayed);
        return true;
    }

    public float ModifyIncoming(Damageable damageable, ref DamageRequest request, float current) {
        if (request.kind == DamageKind.Delayed) return current;

        if (TryDelayIncomingDamage(ref request, current))
            return request.amount;
        return current;
    }
}