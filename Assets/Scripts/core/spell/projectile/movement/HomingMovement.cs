using UnityEngine;

public class HomingMovement : ISpellMovement {
    private readonly int _terrainLayer = LayerMask.NameToLayer("Terrain");

    private readonly SpellData _data;
    private readonly Collider[] _homingTargets = new Collider[32];
    private readonly BaseSpell _spell;
    private readonly Rigidbody _rb;
    private Vector3 _lastDirection;
    private Player _target;

    private const float TargetHeightOffset = 0.75f;
    private const float AimEpsilonSqr = 0.0001f;
    private const float ObstacleProbeRadius = 0.2f;
    private const float ObstacleClearance = 0.35f;
    private const float AvoidLerp = 0.18f;
    private const float MaxTurnDegrees = 25f;
    private const float SideProbeMaxDistance = 3.5f;

    private readonly LayerMask _obstacleMask;
    private int _avoidSign;

    public HomingMovement(BaseSpell s, Rigidbody rb, SpellData data) {
        _spell = s;
        _rb = rb;
        _data = data;
        _obstacleMask = 1 << _terrainLayer;
    }

    public void Initialize() {
        _lastDirection = _spell.transform.forward;
        _rb.linearVelocity = _lastDirection * _data.baseSpeed;
        _avoidSign = 0;
    }

    public void Tick() {
        if (_rb.isKinematic) return;

        if (!IsTargetValid())
            AcquireTarget();

        if (_target != null) {
            var desiredDir = (TargetPosition() - _spell.transform.position);
            if (desiredDir.sqrMagnitude > AimEpsilonSqr) {
                desiredDir.Normalize();

                if (HasLineOfSightToTarget()) {
                    _avoidSign = 0;
                    var toDesired = Vector3.RotateTowards(_lastDirection, desiredDir, Mathf.Deg2Rad * MaxTurnDegrees, 0f);
                    _lastDirection = Vector3.Slerp(_lastDirection, toDesired, 0.35f).normalized;
                }
                else if (TryGetObstacleAvoidanceDirection(desiredDir, out var avoidDir)) {
                    _lastDirection = Vector3.RotateTowards(_lastDirection, avoidDir, Mathf.Deg2Rad * MaxTurnDegrees, 0f);
                }
                else {
                    var toDesired = Vector3.RotateTowards(_lastDirection, desiredDir, Mathf.Deg2Rad * MaxTurnDegrees, 0f);
                    _lastDirection = Vector3.Slerp(_lastDirection, toDesired, 0.2f).normalized;
                }
            }
        }

        _rb.linearVelocity = _lastDirection * _data.baseSpeed;
    }

    private bool IsTargetValid() {
        if (_target == null) return false;
        if (TeamManager.Instance.AreAllies(_target.OwnerClientId, _spell.OwnerClientId)) return false;
        if (!_target.TryGetComponent<OldDamageable>(out var damageable)) return false;
        if (damageable.isDead) return false;
        if (damageable.GetComponentInChildren<Freeze>() != null) return false;

        var delta = TargetPosition() - _spell.transform.position;
        return delta.sqrMagnitude <= _data.homingRadius * _data.homingRadius;
    }

    private Vector3 TargetPosition() {
        return _target.transform.position + Vector3.up * TargetHeightOffset;
    }

    private bool HasLineOfSightToTarget() {
        var origin = _spell.transform.position;
        var target = TargetPosition();
        var toTarget = target - origin;
        var distance = toTarget.magnitude;
        if (distance <= 0.01f) return true;

        var dir = toTarget / distance;
        return !Physics.SphereCast(origin, ObstacleProbeRadius, dir, out _, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private bool TryGetObstacleAvoidanceDirection(Vector3 desiredDir, out Vector3 avoidanceDir) {
        avoidanceDir = desiredDir;

        var origin = _spell.transform.position;
        var target = TargetPosition();
        var toTarget = target - origin;
        var distance = toTarget.magnitude;
        if (distance <= 0.01f) return false;

        var dir = toTarget / distance;

        var hasObstacle = Physics.SphereCast(origin, ObstacleProbeRadius, dir, out var hit, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
        if (!hasObstacle)
            return false;

        if (hit.collider != null && hit.collider.transform == _target.transform)
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

        var probeDistance = Mathf.Min(SideProbeMaxDistance, Mathf.Min(distance, _data.homingRadius));
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
        if (Physics.SphereCast(origin, ObstacleProbeRadius, dir, out _, maxDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            return false;
        if (Physics.SphereCast(origin + dir * ObstacleClearance, ObstacleProbeRadius, dir, out _, maxDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            return false;
        return true;
    }

    private void AcquireTarget() {
        _target = null;

        var size = Physics.OverlapSphereNonAlloc(_spell.transform.position, _data.homingRadius, _homingTargets);
        for (var i = 0; i < size; i++) {
            var col = _homingTargets[i];
            if (!col.TryGetComponent<Player>(out var player)) continue;
            if (TeamManager.Instance.AreAllies(player.OwnerClientId, _spell.OwnerClientId)) continue;
            if (!player.TryGetComponent<OldDamageable>(out var damageable)) continue;
            if (damageable.isDead) continue;
            if (player.GetComponentInChildren<Freeze>() != null) continue;

            _target = player;
            return;
        }
    }
}