using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffects/Forced Movement")]
public class ForcedMovementEffect : StatusEffectData {
    private static readonly RaycastHit[] RaycastHitsBuffer = new RaycastHit[16];

    private static readonly IComparer<RaycastHit> RaycastHitDistanceComparer =
        Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    public enum TargetPointMode {
        CasterPosition = 0,
        ProjectileForwardToWall = 1,
    }

    [SerializeField] private TargetPointMode targetPointMode = TargetPointMode.CasterPosition;
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool inheritInitialProjectileSpeed;
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private float wallBackOffset = 0.3f;
    [SerializeField] private LayerMask wallMask;

    public override StatusEffectRuntime CreateRuntime() {
        return new ForcedMovementRuntime(this);
    }

    public bool TryApplyToTarget(StatusEffectApplyContext applyContext, GameObject target, out float resolvedDuration) {
        var targetPoint = ResolveTargetPoint(applyContext, target);
        resolvedDuration = ResolveMovementDuration(applyContext, target, targetPoint);

        if (target.TryGetComponent<StateController>(out var player)) {
            if (TeamManager.Instance.AreAllies(applyContext.ownerClientId, player.OwnerClientId))
                return false;

            player.StartForcedMovement(targetPoint, resolvedDuration);
            return true;
        }

        if (target.TryGetComponent<Draggable>(out var draggable)) {
            draggable.StartForcedMovement(targetPoint, resolvedDuration, offset);
            return true;
        }

        return false;
    }

    private float ResolveMovementDuration(StatusEffectApplyContext applyContext, GameObject target, Vector3 targetPoint) {
        var speed = ResolveMovementSpeed(applyContext);
        if (speed <= 0.0001f)
            return Mathf.Max(duration, Time.fixedDeltaTime);

        var distance = Vector3.Distance(target.transform.position, targetPoint);
        if (distance <= 0.0001f)
            return Time.fixedDeltaTime;

        return Mathf.Max(distance / speed, Time.fixedDeltaTime);
    }

    private float ResolveMovementSpeed(StatusEffectApplyContext applyContext) {
        if (inheritInitialProjectileSpeed && applyContext.sourceProjectileInitialSpeed > 0.0001f)
            return applyContext.sourceProjectileInitialSpeed;

        return movementSpeed;
    }

    private Vector3 ResolveTargetPoint(StatusEffectApplyContext applyContext, GameObject target) {
        return targetPointMode switch {
            TargetPointMode.CasterPosition => GetCasterPosition(applyContext, target),
            TargetPointMode.ProjectileForwardToWall => GetProjectileForwardWallPosition(applyContext, target),
            _ => target.transform.position
        };
    }

    private Vector3 GetCasterPosition(StatusEffectApplyContext applyContext, GameObject target) {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(applyContext.ownerClientId, out var client) &&
            client.PlayerObject != null)
            return client.PlayerObject.transform.position;

        return target.transform.position;
    }

    private Vector3 GetProjectileForwardWallPosition(StatusEffectApplyContext applyContext, GameObject target) {
        if (applyContext.sourceObject == null)
            return target.transform.position;

        var sourceTransform = applyContext.sourceObject.transform;
        var origin = sourceTransform.position;
        var direction = sourceTransform.forward;
        var distance = Mathf.Max(0.1f, maxDistance);
        var hitCount = Physics.RaycastNonAlloc(origin, direction, RaycastHitsBuffer, distance, GetWallMask(),
            QueryTriggerInteraction.Ignore);
        if (hitCount == 0)
            return origin + direction * distance;

        Array.Sort(RaycastHitsBuffer, 0, hitCount, RaycastHitDistanceComparer);
        for (var i = 0; i < hitCount; i++) {
            var hit = RaycastHitsBuffer[i];
            var hitTransform = hit.collider.transform;
            if (hitTransform.IsChildOf(sourceTransform))
                continue;
            if (hitTransform.IsChildOf(target.transform))
                continue;

            return hit.point - direction * wallBackOffset;
        }

        return origin + direction * distance;
    }

    private int GetWallMask() {
        if (wallMask.value != 0)
            return wallMask.value;

        return Physics.DefaultRaycastLayers & ~(1 << LayerMask.NameToLayer("Player"));
    }

    private class ForcedMovementRuntime : StatusEffectRuntime {
        private readonly ForcedMovementEffect _data;

        public ForcedMovementRuntime(ForcedMovementEffect data) : base(data) {
            _data = data;
        }

        public override void OnApply(StatusEffectApplyContext applyContext, GameObject target) {
            base.OnApply(applyContext, target);
            if (_data.TryApplyToTarget(applyContext, target, out var resolvedDuration))
                _timeRemaining = resolvedDuration;
        }

        public override void OnExpire(GameObject target) {
            base.OnExpire(target);
            if (target.TryGetComponent<StateController>(out var player)) {
                player.StopForcedMovement();
                return;
            }

            if (target.TryGetComponent<Draggable>(out var draggable))
                draggable.StopForcedMovement();
        }
    }
}