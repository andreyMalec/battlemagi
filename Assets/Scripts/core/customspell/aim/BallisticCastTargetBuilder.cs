using UnityEngine;

public static class BallisticCastTargetBuilder {
    public static bool Intercept(
        Vector3 start,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float projectileSpeed,
        float gravity,
        out Vector3 aimPoint
    ) {
        aimPoint = targetPos;
        const int iterations = 5;
        Vector3 predicted = targetPos;

        for (int i = 0; i < iterations; i++) {
            Vector3 toTarget = predicted - start;

            float y = toTarget.y;

            Vector3 flat = new Vector3(
                toTarget.x,
                0f,
                toTarget.z);

            float x = flat.magnitude;
            float v2 = projectileSpeed * projectileSpeed;
            float discriminant = v2 * v2 - gravity * (gravity * x * x + 2f * y * v2);

            if (discriminant < 0f)
                return false;

            float sqrt = Mathf.Sqrt(discriminant);

            // low arc
            float angle = Mathf.Atan2(v2 - sqrt, gravity * x);
            float horizontalSpeed = projectileSpeed * Mathf.Cos(angle);
            float time = x / horizontalSpeed;

            predicted = targetPos + targetVelocity * time;
        }

        aimPoint = predicted;
        return true;
    }

    public static bool FlightTime(
        Vector3 start,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float projectileSpeed,
        float gravity,
        out float flightTime
    ) {
        flightTime = 0;

        Vector3 delta = targetPos - start;

        if (!SolveBallisticArc(
                start,
                targetPos,
                projectileSpeed,
                gravity,
                out _)) {
            return false;
        }

        flightTime = GetBallisticFlightTime(start, targetPos, delta.normalized * projectileSpeed);

        return true;
    }

    public static bool SolveIntercept(
        Vector3 start,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float projectileSpeed,
        float gravity,
        out Vector3 launchVelocity
    ) {
        launchVelocity = default;

        Vector3 predicted = targetPos;

        const int iterations = 5;

        for (int i = 0; i < iterations; i++) {
            if (!SolveBallisticArc(
                    start,
                    predicted,
                    projectileSpeed,
                    gravity,
                    out Vector3 vel)) {
                return false;
            }

            launchVelocity = vel;

            float time = GetBallisticFlightTime(start, predicted, launchVelocity);

            predicted = targetPos + targetVelocity * time;
        }

        return true;
    }

    private static bool SolveBallisticArc(
        Vector3 start,
        Vector3 target,
        float speed,
        float gravity,
        out Vector3 velocity
    ) {
        velocity = default;
        Vector3 delta = target - start;
        float y = delta.y;
        Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);
        float x = deltaXZ.magnitude;
        float v2 = speed * speed;
        float discriminant = v2 * v2 - gravity * (gravity * x * x + 2f * y * v2);

        if (discriminant < 0f)
            return false;

        float sqrt = Mathf.Sqrt(discriminant);
        float lowAngle = Mathf.Atan2(v2 - sqrt, gravity * x);
        Vector3 dir = deltaXZ.normalized;

        velocity =
            dir * (Mathf.Cos(lowAngle) * speed) +
            Vector3.up * (Mathf.Sin(lowAngle) * speed);

        return true;
    }

    private static float GetBallisticFlightTime(
        Vector3 start,
        Vector3 target,
        Vector3 velocity
    ) {
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);

        float horizontalSpeed = flat.magnitude;

        float distance = Vector3.Distance(
            new Vector3(start.x, 0f, start.z),
            new Vector3(target.x, 0f, target.z));

        return distance / horizontalSpeed;
    }

    public static ITarget Build(
        SpellCaster caster, ITarget target, SpellDefinition spell, float targetLift = 0.1f,
        float targetBodyHeightFactor = 0.75f
    ) {
        if (caster == null || target == null || spell == null)
            return target;
        if (spell.coreType != CoreType.Projectile || spell.projectile == null)
            return target;

        var projectile = spell.projectile;
        if (!projectile.enableGravity)
            return target;

        var gravityY = Mathf.Abs(projectile.gravity.y);
        if (gravityY <= 0.01f)
            return target;

        var stats = caster.GetComponent<Stats>();
        var speed = projectile.moveSpeed * (stats?.GetFinal(StatType.ProjectileSpeed) ?? 1f);
        speed = Mathf.Max(0.1f, speed);

        var origin = caster.Origin;
        var targetPos = GetAimPoint(target, targetBodyHeightFactor) + Vector3.up * targetLift;
        var delta = targetPos - origin;
        var planar = new Vector3(delta.x, 0f, delta.z);
        var x = planar.magnitude;
        if (x <= 0.01f)
            return target;

        var y = delta.y;
        var v2 = speed * speed;
        var discriminant = v2 * v2 - gravityY * (gravityY * x * x + 2f * y * v2);
        if (discriminant <= 0f)
            return target;

        var sqrt = Mathf.Sqrt(discriminant);
        var tanHigh = (v2 - sqrt) / (gravityY * x);
        var angle = Mathf.Atan(tanHigh);

        var planarDir = planar / x;
        var sin = Mathf.Sin(angle);
        var cos = Mathf.Cos(angle);
        var aimDir = planarDir * cos + Vector3.up * sin;
        var aimPoint = origin + aimDir.normalized * Mathf.Max(x, 5f);

        return new BallisticCastTarget(target, aimPoint);
    }

    public static Vector3 GetAimPoint(ITarget target, float targetBodyHeightFactor = 0.75f) {
        var targetPos = target.Position;
        if (!target.CanGet)
            return targetPos;

        var targetGo = target.Get;
        if (targetGo.TryGetComponent<CharacterController>(out var characterController)) {
            var worldCenter = targetGo.transform.TransformPoint(characterController.center);
            return worldCenter + characterController.height * (targetBodyHeightFactor - 0.5f) * Vector3.up;
        }

        if (targetGo.TryGetComponent<CapsuleCollider>(out var capsule)) {
            var worldCenter = targetGo.transform.TransformPoint(capsule.center);
            return worldCenter + capsule.height * (targetBodyHeightFactor - 0.5f) * Vector3.up;
        }

        if (targetGo.TryGetComponent<Renderer>(out var targetRenderer)) {
            var bounds = targetRenderer.bounds;
            return bounds.min + bounds.size.y * targetBodyHeightFactor * Vector3.up;
        }

        return targetPos + Vector3.up;
    }
}

public sealed class BallisticCastTarget : ITarget {
    private readonly ITarget _baseTarget;
    private readonly Vector3 _position;

    public BallisticCastTarget(ITarget baseTarget, Vector3 position) {
        _baseTarget = baseTarget;
        _position = position;
    }

    public Vector3 Position => _position;
    public bool IsPlayer => _baseTarget.IsPlayer;
    public bool IsSpell => _baseTarget.IsSpell;
    public ParticipantId OwnerId => _baseTarget.OwnerId;
    public ulong ObjectId => _baseTarget.ObjectId;
    public bool CanGet => _baseTarget.CanGet;
    public GameObject Get => _baseTarget.Get;
}