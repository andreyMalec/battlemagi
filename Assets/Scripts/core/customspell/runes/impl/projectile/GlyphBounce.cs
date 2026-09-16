using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Glyphs/Bounce")]
public class GlyphBounce : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.coreType == CoreType.Projectile) {
            ctx.workingSpell.projectile.enableBounce = true;
            ctx.workingSpell.projectile.maxBounces += 1;
            return true;
        }

        return false;
    }
}