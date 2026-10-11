using UnityEngine;

[CreateAssetMenu(fileName = "Pierce", menuName = "Glyphs/Projectile/Pierce")]
public class GlyphPierce : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.enablePierce = true;
            ctx.workingSpell.projectile.maxPierces += 1;
            return true;
        }

        return false;
    }
}