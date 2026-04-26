using UnityEngine;

public class IceSlideMovementModule : MonoBehaviour {
    private bool _active;
    private bool _seedFromTargetVelocity;
    private float _acceleration;
    private float _deceleration;
    private Vector3 _velocity;

    public bool IsActive => _active;

    public void SetSliding(float acceleration, float deceleration) {
        var wasActive = _active;
        _active = true;
        _acceleration = Mathf.Max(0f, acceleration);
        _deceleration = Mathf.Max(0f, deceleration);
        if (!wasActive)
            _seedFromTargetVelocity = true;
    }

    public void ClearSliding() {
        _active = false;
        _seedFromTargetVelocity = false;
        _velocity = Vector3.zero;
    }

    public Vector3 ResolveVelocity(Vector3 targetVelocity, bool hasInput, float deltaTime) {
        if (!_active || deltaTime <= 0f)
            return targetVelocity;

        if (_seedFromTargetVelocity) {
            _velocity = targetVelocity;
            _seedFromTargetVelocity = false;
        }

        var speed = hasInput ? _acceleration : _deceleration;
        _velocity = Vector3.MoveTowards(_velocity, hasInput ? targetVelocity : Vector3.zero, speed * deltaTime);
        return _velocity;
    }
}
