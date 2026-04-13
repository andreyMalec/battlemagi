using UnityEngine;

public class AcceleratedMoveTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    public SpellMotion Motion {
        get => _motion;
        set {
            _motion = value;
            var velocity = value.Velocity;
            if (velocity.sqrMagnitude > 0f) {
                _direction = velocity.normalized;
                _speed = velocity.magnitude;
            } else {
                _speed = 0f;
            }
        }
    }

    private readonly float _acceleration;

    private ISpellContext _ctx;
    private SpellMotion _motion;
    private Vector3 _direction;
    private float _speed;

    public AcceleratedMoveTransform(Vector3 dir, float speed, float acceleration) {
        _direction = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
        _speed = Mathf.Max(0f, speed);
        _acceleration = acceleration;
        _motion = new SpellMotion { Velocity = _direction * _speed };
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;

        if (_direction.sqrMagnitude <= 0f)
            _direction = transform.forward;

        _motion = new SpellMotion { Velocity = _direction * _speed };
    }

    public void Tick(float dt) {
        var distance = GetTravelDistance(dt);
        if (distance > 0f)
            Transform.position += _direction * (distance * _ctx.Stats.GetFinal(StatType.ProjectileSpeed));

        _speed = GetNextSpeed(dt);
        _motion = new SpellMotion { Velocity = _direction * _speed };

        if (_direction.sqrMagnitude > 0f && distance > 0f)
            Transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
    }

    public Vector3 Sample(float dt) {
        var distance = GetTravelDistance(dt) * _ctx.Stats.GetFinal(StatType.ProjectileSpeed);
        return Transform.position + _direction * distance;
    }

    public void SetForward(Vector3 forward) {
        if (forward.sqrMagnitude > 0f)
            _direction = forward.normalized;
        else if (Transform != null)
            _direction = Transform.forward;

        _motion = new SpellMotion { Velocity = _direction * _speed };
    }

    private float GetNextSpeed(float dt) {
        if (dt <= 0f)
            return _speed;

        return Mathf.Max(0f, _speed + _acceleration * dt);
    }

    private float GetTravelDistance(float dt) {
        if (dt <= 0f)
            return 0f;

        if (_speed <= 0f && _acceleration <= 0f)
            return 0f;

        if (Mathf.Abs(_acceleration) <= 0.0001f)
            return _speed * dt;

        if (_acceleration > 0f)
            return _speed * dt + 0.5f * _acceleration * dt * dt;

        var stopTime = _speed / -_acceleration;
        var moveTime = Mathf.Min(dt, stopTime);
        return Mathf.Max(0f, _speed * moveTime + 0.5f * _acceleration * moveTime * moveTime);
    }
}

