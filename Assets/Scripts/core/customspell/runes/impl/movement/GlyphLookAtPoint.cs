using UnityEngine;

[CreateAssetMenu(fileName = "LookAtPoint", menuName = "Glyphs/Movement/LookAtPoint")]
public class GlyphLookAtPoint : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.IsProjectile()) {
            var proj = ctx.workingSpell.projectile;
            proj.moveType = SpellMovement.LookAtPoint;
            return true;
        }

        if (ctx.IsArea()) {
            var zone  = ctx.workingSpell.zone;
            zone.moveType = SpellMovement.LookAtPoint;
            return true;
        }

        return false;
    }
}