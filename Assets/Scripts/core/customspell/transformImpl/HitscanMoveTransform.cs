using UnityEngine;

public class HitscanMoveTransform : ISpellTransform {
    public Transform Transform { get; private set; }

    private readonly float _range;

    private ISpellContext _ctx;
    private bool _resolved;

    public SpellMotion Motion { get; set; }

    public HitscanMoveTransform(Vector3 dir, float moveSpeed, bool enableMaxDistance, float maxDistance) {
        var direction = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
        var range = enableMaxDistance ? maxDistance : moveSpeed;
        if (range <= 0f)
            range = moveSpeed;
        Motion = new SpellMotion { Velocity = direction * Mathf.Max(0f, range) };
        _range = Mathf.Max(0f, range);
    }

    public void Init(Transform transform, ISpellContext ctx) {
        Transform = transform;
        _ctx = ctx;
        _resolved = false;
    }

    public void Tick(float dt) {
        if (_resolved)
            return;

        Transform.position = EvaluatePosition();
        var dir = Motion.Velocity;
        if (dir.sqrMagnitude > 0f)
            Transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        _resolved = true;
        Motion = new SpellMotion { Velocity = Vector3.zero };
    }

    public Vector3 Sample(float dt) {
        return EvaluatePosition();
    }

    public void SetForward(Vector3 forward) {
        var speed = Motion.Velocity.magnitude;
        var dir = forward.sqrMagnitude > 0f ? forward.normalized : Transform.forward;
        Motion = new SpellMotion { Velocity = dir * speed };
    }

    private Vector3 EvaluatePosition() {
        if (_range <= 0f)
            return Transform.position;

        var velocity = Motion.Velocity;
        if (velocity.sqrMagnitude <= 0f)
            return Transform.position;

        var direction = velocity.normalized;
        var distance = velocity.magnitude * _ctx.Stats.GetFinal(StatType.ProjectileSpeed);
        if (distance <= 0f)
            return Transform.position;

        var nextPosition = Transform.position + direction * distance;
        return nextPosition;
    }
}

