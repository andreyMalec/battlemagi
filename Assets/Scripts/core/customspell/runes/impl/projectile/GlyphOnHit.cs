using UnityEngine;

[CreateAssetMenu(fileName = "OnHit", menuName = "Glyphs/OnHit")]
public class GlyphOnHit : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.coreType == CoreType.Projectile) {
            if (ctx.nestedSpell == null) {
                ctx.AddError("OnHit trigger requires nested spell.");
                return false;
            }

            ctx.workingSpell.projectile.onHitSpawn = ctx.nestedSpell;
            return true;
        }

        return false;
    }
}