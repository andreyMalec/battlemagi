using UnityEngine;

[CreateAssetMenu(fileName = "Collapse", menuName = "Glyphs/Knockback/Collapse")]
public class GlyphCollapse : RuneGlyphDefinition {
    [Header("Properties")]
    public float impulse = 8f;

    public float forcePerSecond = 10f;
    public float duration = 0.5f;

    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.knockback = CreateInstance<KnockbackDefinition>();
        ctx.workingSpell.knockback.mode = SpellKnockbackMode.Impulse;
        if (ctx.coreGlyph is GlyphCloud) {
            ctx.workingSpell.knockback.mode = SpellKnockbackMode.Continuous;
        }

        ctx.workingSpell.knockback.vectorMode = SpellKnockbackVectorMode.TowardPoint;
        ctx.workingSpell.knockback.impulse = impulse;
        ctx.workingSpell.knockback.forcePerSecond = forcePerSecond;
        ctx.workingSpell.knockback.duration = duration;
        ctx.workingSpell.knockback.canHitAllies = true;

        return true;
    }
}