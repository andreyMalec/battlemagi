using UnityEngine;

[CreateAssetMenu(fileName = "Fire", menuName = "Glyphs/Fire")]
public class GlyphFire : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.coreType == CoreType.Projectile) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileFire;
            return true;
        }
        if (ctx.workingSpell.coreType == CoreType.Zone) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionFire;
            } else if (ctx.coreGlyph is GlyphCloud) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneFire;
            } else if (ctx.coreGlyph is GlyphPulse) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionFire;
            } else {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionFire;
            }
            return true;
        }
        
        return false;
    }
}