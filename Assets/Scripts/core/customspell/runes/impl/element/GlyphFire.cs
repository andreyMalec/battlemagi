using UnityEngine;

[CreateAssetMenu(fileName = "Fire", menuName = "Glyphs/Fire")]
public class GlyphFire : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.coreType == CoreType.Projectile) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileFire;
            return true;
        }
        if (ctx.workingSpell.coreType == CoreType.Zone) {
            ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionFire;
            return true;
        }
        
        return false;
    }
}