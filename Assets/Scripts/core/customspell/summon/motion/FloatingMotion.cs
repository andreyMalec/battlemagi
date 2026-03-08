using UnityEngine;

public class FloatingMotion : ILocomotion {
    private readonly float _speed;
    private readonly float _minDistance;
    private readonly float _groundHeight;
    private readonly LayerMask _groundMask;
    private readonly float _raycastDistance;

    public FloatingMotion(
        float speed,
        float minDistance = 0f,
        float groundHeight = 0f,
        LayerMask groundMask = default,
        float raycastDistance = 100f
    ) {
        _speed = speed;
        _minDistance = minDistance;
        _groundHeight = groundHeight;
        _groundMask = groundMask;
        _raycastDistance = raycastDistance;
    }

    public void Move(AIContext ctx, Vector3 target) {
        var pos = ctx.Self.position;

        var targetPlanar = new Vector3(target.x, pos.y, target.z);
        var dir = targetPlanar - pos;
        var dist = new Vector3(dir.x, 0f, dir.z).magnitude;

        if (dist <= _minDistance) {
            if (ctx.ActiveTarget == null) return;
            dir = ctx.ActiveTarget.Position - pos;
            if (dir.sqrMagnitude > 0f)
                ctx.Self.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            return;
        }

        var desired = dist - _minDistance;
        var step = Mathf.Min(desired, _speed * Time.deltaTime);
        if (step <= 0f) return;

        var planar = new Vector3(dir.x, 0f, dir.z);
        var nextPos = pos + (planar / dist) * step;
        nextPos = ApplyGroundHeight(nextPos);
        ctx.Self.position = nextPos;

        if (planar.sqrMagnitude > 0f)
            ctx.Self.rotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
    }

    private Vector3 ApplyGroundHeight(Vector3 pos) {
        if (_groundHeight <= 0f) return pos;

        var origin = pos + Vector3.up * _raycastDistance;
        if (Physics.Raycast(origin, Vector3.down, out var hit, _raycastDistance * 2f, _groundMask == default ? ~0 : _groundMask,
                QueryTriggerInteraction.Ignore)) {
            pos.y = hit.point.y + _groundHeight;
        }

        return pos;
    }

    public void Stop() {
    }
}