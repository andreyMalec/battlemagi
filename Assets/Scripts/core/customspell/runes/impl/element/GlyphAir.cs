using UnityEngine;

[CreateAssetMenu(fileName = "Air", menuName = "Glyphs/Element/Air")]
public class GlyphAir : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileAir;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionAir;
            } else if (ctx.coreGlyph is GlyphCloud) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneAir;
            } else if (ctx.coreGlyph is GlyphPulse) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionAir;
            } else {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionAir;
            }
            return true;
        }

        return false;
    }
}