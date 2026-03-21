using UnityEngine;

public static class ParticleUtils {
    static AnimationCurve ScaleCurve(AnimationCurve c, float k) {
        if (c == null)
            return null;

        var keys = c.keys;
        for (int i = 0; i < keys.Length; i++) {
            var key = keys[i];
            key.value *= k;
            key.inTangent *= k;
            key.outTangent *= k;
            keys[i] = key;
        }

        c.keys = keys;
        return c;
    }

    static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve c, float k) {
        switch (c.mode) {
            case ParticleSystemCurveMode.Constant:
                c.constant *= k;
                return c;
            case ParticleSystemCurveMode.TwoConstants:
                c.constantMin *= k;
                c.constantMax *= k;
                return c;
            case ParticleSystemCurveMode.Curve:
                c.curve = ScaleCurve(c.curve, k);
                return c;
            case ParticleSystemCurveMode.TwoCurves:
                c.curveMin = ScaleCurve(c.curveMin, k);
                c.curveMax = ScaleCurve(c.curveMax, k);
                return c;
            default:
                return c;
        }
    }

    public static void Scale(ParticleSystem ps, float k, bool scaleShape = false) {
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
            vel.x = ScaleCurve(vel.x, k);
            vel.y = ScaleCurve(vel.y, k);
            vel.z = ScaleCurve(vel.z, k);
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
            if (scaleShape)
                shape.scale *= k;
        }
    }
}