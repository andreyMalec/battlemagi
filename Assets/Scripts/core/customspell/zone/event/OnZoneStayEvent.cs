using System.Collections.Generic;
using UnityEngine;

public sealed class OnZoneStayEvent : SpellEvent {
    private readonly struct StatusTargetCache {
        public readonly bool IsResolved;
        public readonly Statusable Statusable;
        public readonly ParticipantId OwnerId;

        public StatusTargetCache(bool isResolved, Statusable statusable, ParticipantId ownerId) {
            IsResolved = isResolved;
            Statusable = statusable;
            OwnerId = ownerId;
        }
    }

    private readonly struct DamageTargetCache {
        public readonly bool IsResolved;
        public readonly Damageable Damageable;
        public readonly ParticipantId OwnerId;

        public DamageTargetCache(bool isResolved, Damageable damageable, ParticipantId ownerId) {
            IsResolved = isResolved;
            Damageable = damageable;
            OwnerId = ownerId;
        }
    }

    private Dictionary<GameObject, StatusTargetCache> _statusTargets;
    private Dictionary<GameObject, DamageTargetCache> _damageTargets;

    public IEnumerable<ShapeHit> Targets;
    public float DeltaTime;
    public bool IsInitial;

    public OnZoneStayEvent(IEnumerable<ShapeHit> targets, float deltaTime, bool isInitial = false) {
        Targets = targets;
        DeltaTime = deltaTime;
        IsInitial = isInitial;
    }

    public bool TryGetStatusable(GameObject target, out Statusable statusable, out ParticipantId ownerId) {
        statusable = null;
        ownerId = default;
        if (target == null)
            return false;

        _statusTargets ??= new Dictionary<GameObject, StatusTargetCache>();
        if (!_statusTargets.TryGetValue(target, out var cached)) {
            var isResolved = SpellEffectResolver.TryGetStatusable(target, out var resolvedStatusable, out var resolvedOwnerId);
            cached = new StatusTargetCache(isResolved, resolvedStatusable, resolvedOwnerId);
            _statusTargets[target] = cached;
        }

        if (!cached.IsResolved)
            return false;

        statusable = cached.Statusable;
        ownerId = cached.OwnerId;
        return true;
    }

    public bool TryGetDamageable(GameObject target, out Damageable damageable, out ParticipantId ownerId) {
        damageable = null;
        ownerId = default;
        if (target == null)
            return false;

        _damageTargets ??= new Dictionary<GameObject, DamageTargetCache>();
        if (!_damageTargets.TryGetValue(target, out var cached)) {
            var isResolved = DamageUtils.TryGetOwnerFromCollider(target, out var resolvedDamageable, out var resolvedOwnerId);
            cached = new DamageTargetCache(isResolved, resolvedDamageable, resolvedOwnerId);
            _damageTargets[target] = cached;
        }

        if (!cached.IsResolved)
            return false;

        damageable = cached.Damageable;
        ownerId = cached.OwnerId;
        return true;
    }
}