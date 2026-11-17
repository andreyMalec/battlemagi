using UnityEngine;

public abstract class ElementGlyphBase : RuneGlyphDefinition {
    [Header("Damage")]
    public float projectileDamage = 45f;

    public float areaDamage = 35f;
    public float areaDamageTickInterval = 1f;

    protected virtual void ApplyBase(RuneGlyphContext ctx) {
    }

    protected virtual void ApplyProjectile(RuneGlyphContext ctx) {
    }

    protected virtual void ApplyArea(RuneGlyphContext ctx) {
    }

    protected virtual void ApplyExplosion(RuneGlyphContext ctx) {
    }

    protected virtual void ApplyCloud(RuneGlyphContext ctx) {
    }

    protected virtual void ApplyPulse(RuneGlyphContext ctx) {
    }

    public override bool Apply(RuneGlyphContext ctx) {
        ApplyBase(ctx);

        if (ctx.IsProjectile()) {
            ApplyProjectile(ctx);
            return true;
        }

        if (ctx.IsArea()) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ApplyExplosion(ctx);
            } else if (ctx.coreGlyph is GlyphCloud) {
                ApplyCloud(ctx);
            } else if (ctx.coreGlyph is GlyphPulse) {
                ApplyPulse(ctx);
            } else {
                return false;
            }

            ApplyArea(ctx);

            return true;
        }

        return false;
    }
}