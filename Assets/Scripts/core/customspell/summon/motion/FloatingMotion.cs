using UnityEngine;

public class FloatingMotion : ILocomotion {
    private readonly float _speed;
    private readonly float _minDistance;

    public FloatingMotion(float speed, float minDistance = 0f) {
        _speed = speed;
        _minDistance = minDistance;
    }

    public void Move(AIContext ctx, Vector3 target) {
        var pos = ctx.Self.position;
        var dir = target - pos;
        var dist = dir.magnitude;
        if (dist <= _minDistance) return;

        var desired = dist - _minDistance;
        var step = Mathf.Min(desired, _speed * Time.deltaTime);
        if (step <= 0f) return;

        ctx.Self.position = pos + dir * (step / dist);
        if (dir.sqrMagnitude > 0f)
            ctx.Self.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    public void Stop() {
    }
}
