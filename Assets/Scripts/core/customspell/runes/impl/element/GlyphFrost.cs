using UnityEngine;

[CreateAssetMenu(fileName = "Frost", menuName = "Glyphs/Frost")]
public class GlyphFrost : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.coreType == CoreType.Projectile) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileIce;
            return true;
        }

        if (ctx.workingSpell.coreType == CoreType.Zone) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ctx.AddError("Frost cannot be applied to Explosion");
                return false;
            } else if (ctx.coreGlyph is GlyphCloud) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneFrost;
            } else if (ctx.coreGlyph is GlyphPulse) {
                ctx.AddError("Frost cannot be applied to Pulse");
                return false;
            } else {
                return false;
            }
            return true;
        }

        return false;
    }
}