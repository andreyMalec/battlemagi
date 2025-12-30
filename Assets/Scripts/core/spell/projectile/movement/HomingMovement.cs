using UnityEngine;

public class HomingMovement : ISpellMovement {
    private readonly SpellData _data;
    private readonly Collider[] _homingTargets = new Collider[32];
    private readonly BaseSpell _spell;
    private readonly Rigidbody _rb;
    private Vector3 _lastDirection;
    private Player _target;

    public HomingMovement(BaseSpell s, Rigidbody rb, SpellData data) {
        _spell = s;
        _rb = rb;
        _data = data;
    }

    public void Initialize() {
        _lastDirection = _spell.transform.forward;
        _rb.linearVelocity = _lastDirection * _data.baseSpeed;
    }

    public void Tick() {
        if (_rb.isKinematic) return;

        if (!IsTargetValid())
            AcquireTarget();

        if (_target != null) {
            var toTarget = TargetPosition() - _spell.transform.position;
            if (toTarget.sqrMagnitude > 0.0001f)
                _lastDirection = toTarget.normalized;
        }

        _rb.linearVelocity = _lastDirection * _data.baseSpeed;
    }

    private bool IsTargetValid() {
        if (_target == null) return false;
        if (TeamManager.Instance.AreAllies(_target.OwnerClientId, _spell.OwnerClientId)) return false;
        if (!_target.TryGetComponent<Damageable>(out var damageable)) return false;
        if (damageable.isDead) return false;

        var delta = TargetPosition() - _spell.transform.position;
        return delta.sqrMagnitude <= _data.homingRadius * _data.homingRadius;
    }

    private Vector3 TargetPosition() {
        return _target.transform.position + Vector3.up * 0.75f;
    }

    private void AcquireTarget() {
        _target = null;

        var size = Physics.OverlapSphereNonAlloc(_spell.transform.position, _data.homingRadius, _homingTargets);
        for (var i = 0; i < size; i++) {
            var col = _homingTargets[i];
            if (!col.TryGetComponent<Player>(out var player)) continue;
            if (TeamManager.Instance.AreAllies(player.OwnerClientId, _spell.OwnerClientId)) continue;
            if (!player.TryGetComponent<Damageable>(out var damageable)) continue;
            if (damageable.isDead) continue;

            _target = player;
            return;
        }
    }
}