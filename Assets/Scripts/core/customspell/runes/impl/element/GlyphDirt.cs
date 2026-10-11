using UnityEngine;

[CreateAssetMenu(fileName = "Dirt", menuName = "Glyphs/Element/Dirt")]
public class GlyphDirt : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileDirt;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionDirt;
            } else if (ctx.coreGlyph is GlyphCloud) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneDirt;
            } else if (ctx.coreGlyph is GlyphPulse) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionDirt;
            } else {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionDirt;
            }
            return true;
        }

        return false;
    }
}