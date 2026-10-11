using UnityEngine;

[CreateAssetMenu(fileName = "Shadow", menuName = "Glyphs/Element/Shadow")]
public class GlyphShadow : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileEmpty;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            if (ctx.coreGlyph is GlyphExplosion) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionShadow;
            } else if (ctx.coreGlyph is GlyphCloud) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneShadow;
            } else if (ctx.coreGlyph is GlyphPulse) {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionShadow;
            } else {
                ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionShadow;
            }
            return true;
        }

        return false;
    }
}