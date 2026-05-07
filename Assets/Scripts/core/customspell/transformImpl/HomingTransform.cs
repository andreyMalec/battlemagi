using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HomingTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion {
        get => _inner.Motion;
        set => _inner.Motion = value;
    }

    private readonly ISpellTransform _inner;
    private readonly IWorldQuery _worldQuery;

    private readonly float _maxTurnDegrees;
    private readonly float _aimSlerp;
    private readonly float _homingRadius;

    private const float AimEpsilonSqr = 0.0001f;
    private const float ObstacleProbeRadius = 0.2f;
    private const float ObstacleClearance = 0.35f;
    private const float AvoidLerp = 0.18f;
    private const float SideProbeMaxDistance = 3.5f;

    private readonly LayerMask _obstacleMask;
    private int _avoidSign;

    private Vector3 _lastDirection;
    private ITarget _target;
    private ISpellContext _ctx;

    public HomingTransform(
        ISpellTransform inner,
        float maxTurnDegrees,
        float slerp,
        float homingRadius,
        LayerMask obstacleMask,
        IWorldQuery worldQuery
    ) {
        _inner = inner;
        _maxTurnDegrees = maxTurnDegrees;
        _aimSlerp = slerp;
        _homingRadius = homingRadius;
        _obstacleMask = obstacleMask;
        _worldQuery = worldQuery;
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _inner.Init(transform, ctx);
        _ctx = ctx;

        var v = _inner.Motion.Velocity;
        _lastDirection = v.sqrMagnitude > 0f ? v.normalized : transform.forward;
        _avoidSign = 0;
    }

    public void Tick(float dt) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.HomingTick);
        if (dt <= 0f) {
            _inner.Tick(dt);
            return;
        }

        if (!IsTargetValid()) {
            using var __ = SpellMetrics.Measure(SpellMetricSection.HomingAcquireTarget);
            AcquireTarget();
        }

        if (_target != null) {
            var desiredDir = TargetPosition() - Transform.position;
            if (desiredDir.sqrMagnitude > AimEpsilonSqr) {
                desiredDir.Normalize();

                bool hasLineOfSight;
                using (SpellMetrics.Measure(SpellMetricSection.HomingLineOfSight))
                    hasLineOfSight = HasLineOfSightToTarget();

                if (hasLineOfSight) {
                    _avoidSign = 0;
                    var toDesired =
                        Vector3.RotateTowards(_lastDirection, desiredDir, Mathf.Deg2Rad * _maxTurnDegrees, 0f);
                    _lastDirection = Vector3.Slerp(_lastDirection, toDesired, _aimSlerp).normalized;
                } else {
                    Vector3 avoidDir;
                    bool hasAvoidance;
                    using (SpellMetrics.Measure(SpellMetricSection.HomingObstacleAvoidance))
                        hasAvoidance = TryGetObstacleAvoidanceDirection(desiredDir, out avoidDir);

                    if (hasAvoidance) {
                        _lastDirection =
                            Vector3.RotateTowards(_lastDirection, avoidDir, Mathf.Deg2Rad * _maxTurnDegrees, 0f);
                    } else {
                        var toDesired =
                            Vector3.RotateTowards(_lastDirection, desiredDir, Mathf.Deg2Rad * _maxTurnDegrees, 0f);
                        _lastDirection = Vector3.Slerp(_lastDirection, toDesired, 0.2f).normalized;
                    }
                }

                _inner.SetForward(_lastDirection);
            }
        }

        _inner.Tick(dt);
    }

    public Vector3 Sample(float dt) {
        return _inner.Sample(dt);
    }

    public void SetForward(Vector3 forward) {
        if (forward.sqrMagnitude > 0f)
            _lastDirection = forward.normalized;
        _inner.SetForward(forward);
    }

    private bool IsTargetValid() {
        if (_target == null) return false;
        if (DamageRelationship.AreAllies(_ctx, _target.OwnerId, _target.Get)) return false;
        if (!_target.Get.TryGetComponent<Damageable>(out var damageable)) return false;
        if (damageable.IsDead) return false;
        if (damageable.GetComponentInChildren<Freeze>() != null) return false;

        var delta = TargetPosition() - Transform.position;
        return delta.sqrMagnitude <= _homingRadius * _homingRadius;
    }

    private void AcquireTarget() {
        _target = null;
        var candidates = _worldQuery.FindEnemiesInRadius(Transform.position, _homingRadius);
        _target = FilterTargets(candidates).ToList().FirstOrDefault();
    }

    private IEnumerable<ITarget> FilterTargets(IEnumerable<ITarget> targets) {
        return targets.Filter(it => {
            if (it == (ITarget)_ctx.Caster) return false;
            if (TeamManager.Instance == null)
                return it.OwnerId != _ctx.Caster.OwnerId;
            if (!it.CanGet) return false;
            if (DamageRelationship.AreAllies(_ctx, it.OwnerId, it.Get)) return false;

            return it.IsPlayer
                   && it.Get.TryGetComponent<Damageable>(out var damageable) && !damageable.IsDead
                   && damageable.GetComponentInChildren<Freeze>() == null;
        });
    }

    private Vector3 TargetPosition() {
        if (_target == null) return Transform.position;
        return _target.Position + Vector3.up;
    }

    private bool HasLineOfSightToTarget() {
        var origin = Transform.position;
        var target = TargetPosition();
        var toTarget = target - origin;
        var distance = toTarget.magnitude;
        if (distance <= 0.01f) return true;

        var dir = toTarget / distance;
        return !Physics.SphereCast(origin, ObstacleProbeRadius, dir, out _, distance, _obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool TryGetObstacleAvoidanceDirection(Vector3 desiredDir, out Vector3 avoidanceDir) {
        avoidanceDir = desiredDir;

        var origin = Transform.position;
        var target = TargetPosition();
        var toTarget = target - origin;
        var distance = toTarget.magnitude;
        if (distance <= 0.01f) return false;

        var dir = toTarget / distance;

        var hasObstacle = Physics.SphereCast(origin, ObstacleProbeRadius, dir, out var hit, distance, _obstacleMask,
            QueryTriggerInteraction.Ignore);
        if (!hasObstacle)
            return false;

        var normal = hit.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude > 0.0001f)
            normal.Normalize();
        else {
            normal = Vector3.Cross(Vector3.up, dir);
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.0001f) return false;
            normal.Normalize();
        }

        var tangent = Vector3.Cross(Vector3.up, normal);
        tangent.y = 0f;
        if (tangent.sqrMagnitude < 0.0001f) return false;
        tangent.Normalize();

        var left = tangent;
        var right = -tangent;

        var probeDistance = Mathf.Min(SideProbeMaxDistance, Mathf.Min(distance, _homingRadius));
        var leftOk = IsDirectionClear(origin, left, probeDistance);
        var rightOk = IsDirectionClear(origin, right, probeDistance);

        if (!leftOk && !rightOk)
            return false;

        var preferred = SelectPreferredSide(desiredDir, left, right, leftOk, rightOk);
        var steer = Vector3.Slerp(desiredDir, preferred, AvoidLerp).normalized;

        avoidanceDir = steer;
        return true;
    }

    private Vector3 SelectPreferredSide(Vector3 desiredDir, Vector3 left, Vector3 right, bool leftOk, bool rightOk) {
        if (_avoidSign != 0) {
            if (_avoidSign < 0 && leftOk) return left;
            if (_avoidSign > 0 && rightOk) return right;
            _avoidSign = 0;
        }

        if (leftOk && !rightOk) {
            _avoidSign = -1;
            return left;
        }

        if (rightOk && !leftOk) {
            _avoidSign = 1;
            return right;
        }

        var leftScore = Vector3.Dot(_lastDirection, left) + Vector3.Dot(desiredDir, left) * 0.25f;
        var rightScore = Vector3.Dot(_lastDirection, right) + Vector3.Dot(desiredDir, right) * 0.25f;

        if (leftScore >= rightScore) {
            _avoidSign = -1;
            return left;
        }

        _avoidSign = 1;
        return right;
    }

    private bool IsDirectionClear(Vector3 origin, Vector3 dir, float maxDistance) {
        if (Physics.SphereCast(origin, ObstacleProbeRadius, dir, out _, maxDistance, _obstacleMask,
                QueryTriggerInteraction.Ignore))
            return false;
        if (Physics.SphereCast(origin + dir * ObstacleClearance, ObstacleProbeRadius, dir, out _, maxDistance,
                _obstacleMask, QueryTriggerInteraction.Ignore))
            return false;
        return true;
    }
}