using UnityEngine;

public static class BallisticCastTargetBuilder {
    public static ITarget Build(SpellCaster caster, ITarget target, SpellDefinition spell, float targetLift = 0.1f,
        float targetBodyHeightFactor = 0.75f) {
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
        var tanHigh = (v2 + sqrt) / (gravityY * x);
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
    public OwnerId OwnerId => _baseTarget.OwnerId;
    public ulong ObjectId => _baseTarget.ObjectId;
    public bool CanGet => _baseTarget.CanGet;
    public GameObject Get => _baseTarget.Get;
}

