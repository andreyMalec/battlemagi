using UnityEngine;

[CreateAssetMenu(fileName = "Fire", menuName = "Glyphs/Element/Fire")]
public class GlyphFire : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileFire;
            var dmg  = CreateInstance<DamageDefinition>();
            dmg.amount = 30;
            ctx.workingSpell.damage = dmg;
            return true;
        }
        if (ctx.HasTag(RuneGlyphTag.Area)) {
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