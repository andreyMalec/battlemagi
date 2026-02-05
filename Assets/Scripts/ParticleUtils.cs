using UnityEngine;

public static class ParticleUtils {
    public static void Scale(ParticleSystem ps, float k) {
        var main = ps.main;
        main.startSizeMultiplier *= k;
        main.startSpeedMultiplier *= k;
        main.gravityModifierMultiplier *= k;

        var limit = ps.limitVelocityOverLifetime;
        if (limit.enabled) {
            limit.limitMultiplier *= k;
            limit.dampen *= k;
        }

        var vel = ps.velocityOverLifetime;
        if (vel.enabled) {
            vel.xMultiplier *= k;
            vel.yMultiplier *= k;
            vel.zMultiplier *= k;
        }

        var inherit = ps.inheritVelocity;
        if (inherit.enabled)
            inherit.curveMultiplier *= k;

        var force = ps.forceOverLifetime;
        if (force.enabled) {
            force.xMultiplier *= k;
            force.yMultiplier *= k;
            force.zMultiplier *= k;
        }

        var rot = ps.rotationOverLifetime;
        if (rot.enabled)
            rot.zMultiplier *= k;

        var rotBySpeed = ps.rotationBySpeed;
        if (rotBySpeed.enabled)
            rotBySpeed.zMultiplier *= k;

        var shape = ps.shape;
        if (shape.enabled) {
            shape.radius *= k;
            shape.radiusThickness *= k;
            shape.arc *= k;
            shape.length *= k;
            shape.boxThickness *= k;
        }

    }
}