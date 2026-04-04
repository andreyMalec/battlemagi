using UnityEngine;

public static class SpellKnockbackDirectionUtility {
    public static Vector3 ComputeDirection(Transform origin, Vector3 point, SpellKnockbackVectorMode vectorMode, float upBias) {
        var position = origin.position;
        var direction = vectorMode switch {
            SpellKnockbackVectorMode.TowardPoint => point - position,
            SpellKnockbackVectorMode.TowardPointAndUp => ComputeTowardPointAndUp(position, point, upBias),
            _ => position - point,
        };

        if (direction.sqrMagnitude < 0.0001f) {
            direction = vectorMode is SpellKnockbackVectorMode.AwayFromPoint
                ? origin.forward
                : Vector3.up;
        }

        return direction.normalized;
    }

    private static Vector3 ComputeTowardPointAndUp(Vector3 position, Vector3 point, float upBias) {
        var toward = point - position;
        var horizontalToward = Vector3.ProjectOnPlane(toward, Vector3.up);
        return horizontalToward + Vector3.up * upBias;
    }
}
