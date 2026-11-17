using UnityEngine;

[CreateAssetMenu(fileName = "Slow", menuName = "Glyphs/Slow")]
public class GlyphSlow : RuneGlyphDefinition {
    [Header("Properties")]
    public float projMulti = .5f;
    public float zoneMulti = .5f;
    
    
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.IsProjectile()) {
            var proj = ctx.workingSpell.projectile;
            proj.moveSpeed *= projMulti;
            return true;
        }

        if (ctx.IsArea()) {
            var zone = ctx.workingSpell.zone;
            zone.moveSpeed *= zoneMulti;
            return true;
        }

        return true;
    }
}