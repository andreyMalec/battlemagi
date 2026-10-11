using UnityEngine;

[CreateAssetMenu(fileName = "Lightning", menuName = "Glyphs/Element/Lightning")]
public class GlyphLightning : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileLightning;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionLightning;
            } else if (ctx.coreGlyph is GlyphCloud) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneLightning;
            } else if (ctx.coreGlyph is GlyphPulse) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionLightning;
            } else {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionLightning;
            }
            return true;
        }

        return false;
    }
}