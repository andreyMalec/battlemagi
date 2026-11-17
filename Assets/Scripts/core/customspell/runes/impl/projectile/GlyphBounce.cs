using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Glyphs/Projectile/Bounce")]
public class GlyphBounce : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.IsProjectile()) {
            ctx.workingSpell.projectile.enableBounce = true;
            ctx.workingSpell.projectile.maxBounces += 1;
            return true;
        }

        return false;
    }
}