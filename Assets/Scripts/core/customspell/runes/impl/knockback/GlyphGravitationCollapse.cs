using UnityEngine;

[CreateAssetMenu(fileName = "GravitationCollapse", menuName = "Glyphs/Knockback/GravitationCollapse")]
public class GlyphGravitationCollapse : RuneGlyphDefinition {
    [Header("Properties")]
    public float impulse = 8f;

    public float forcePerSecond = 10f;
    public float duration = 0.5f;
    public float upBias = 1f;

    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.knockback = CreateInstance<KnockbackDefinition>();
        ctx.workingSpell.knockback.mode = SpellKnockbackMode.Impulse;
        if (ctx.coreGlyph is GlyphCloud) {
            ctx.workingSpell.knockback.mode = SpellKnockbackMode.Continuous;
        }

        ctx.workingSpell.knockback.vectorMode = SpellKnockbackVectorMode.TowardPointAndUp;
        ctx.workingSpell.knockback.impulse = impulse;
        ctx.workingSpell.knockback.forcePerSecond = forcePerSecond;
        ctx.workingSpell.knockback.duration = duration;
        ctx.workingSpell.knockback.upBias = upBias;
        ctx.workingSpell.knockback.canHitAllies = true;

        return true;
    }
}