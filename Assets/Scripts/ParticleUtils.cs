using UnityEngine;

public static class ParticleUtils {
    public const float ConeEffectiveLengthScale = 1.25f;

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

    public static float GetEffectiveConeLength(BeamDefinition def) {
        return Mathf.Max(0f, def.coneLength * ConeEffectiveLengthScale);
    }

    public static float GetConeEndRadius(BeamDefinition def) {
        var startRadius = Mathf.Max(0f, def.coneRadius);
        var angle = Mathf.Clamp(def.coneAngle, 0f, 89f);
        return startRadius + Mathf.Tan(angle * Mathf.Deg2Rad) * GetEffectiveConeLength(def);
    }

    public static void ApplyBeamShape(GameObject root, BeamDefinition def) {
        if (def == null || def.shapeType is not BeamShapeType.Cone)
            return;

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) {
            ApplyConeBeamShape(ps, def);
        }
    }

    public static void ApplyConeBeamShape(ParticleSystem ps, BeamDefinition def) {
        var shape = ps.shape;
        if (shape.shapeType != ParticleSystemShapeType.Cone)
            return;

        shape.radius = def.coneRadius;
        shape.angle = def.coneAngle;

        var effectiveLength = GetEffectiveConeLength(def);
        var forwardSpeed = GetMaxConeForwardSpeed(ps, def.coneAngle);
        if (forwardSpeed <= 0.0001f) {
            shape.length = effectiveLength;
            return;
        }

        shape.length = 0f;

        var main = ps.main;
        var lifetime = effectiveLength / forwardSpeed;
        var currentLifetime = GetMaxPositiveCurveValue(main.startLifetime);
        main.startLifetime = currentLifetime <= 0.0001f
            ? new ParticleSystem.MinMaxCurve(lifetime)
            : ScaleCurve(main.startLifetime, lifetime / currentLifetime);
    }

    static float GetMaxConeForwardSpeed(ParticleSystem ps, float coneAngle) {
        var forwardFactor = Mathf.Cos(Mathf.Clamp(coneAngle, 0f, 89f) * Mathf.Deg2Rad);
        var forwardSpeed = GetMaxPositiveCurveValue(ps.main.startSpeed) * forwardFactor;

        var velocity = ps.velocityOverLifetime;
        if (velocity.enabled)
            forwardSpeed += GetMaxPositiveCurveValue(velocity.z);

        return forwardSpeed;
    }

    static float GetMaxPositiveCurveValue(ParticleSystem.MinMaxCurve curve) {
        return curve.mode switch {
            ParticleSystemCurveMode.Constant => Mathf.Max(0f, curve.constant),
            ParticleSystemCurveMode.TwoConstants => Mathf.Max(0f, curve.constantMin, curve.constantMax),
            ParticleSystemCurveMode.Curve => GetMaxPositiveCurveValue(curve.curve, curve.curveMultiplier),
            ParticleSystemCurveMode.TwoCurves => Mathf.Max(
                GetMaxPositiveCurveValue(curve.curveMin, curve.curveMultiplier),
                GetMaxPositiveCurveValue(curve.curveMax, curve.curveMultiplier)
            ),
            _ => 0f
        };
    }

    static float GetMaxPositiveCurveValue(AnimationCurve curve, float multiplier) {
        if (curve == null)
            return 0f;

        var maxValue = 0f;
        const int samples = 16;
        for (var i = 0; i <= samples; i++) {
            var time = i / (float)samples;
            maxValue = Mathf.Max(maxValue, curve.Evaluate(time) * multiplier);
        }

        return Mathf.Max(0f, maxValue);
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