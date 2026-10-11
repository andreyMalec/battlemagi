using UnityEngine;

[CreateAssetMenu(fileName = "Linear", menuName = "Glyphs/Movement/Linear")]
public class GlyphLinear : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            var proj = ctx.workingSpell.projectile;
            proj.moveType = SpellMovement.Linear;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            var zone  = ctx.workingSpell.zone;
            zone.moveType = SpellMovement.Linear;
            return true;
        }

        return false;
    }
}