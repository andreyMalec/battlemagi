using UnityEngine;

[CreateAssetMenu(fileName = "Ice", menuName = "Glyphs/Ice")]
public class GlyphIce : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.coreType == CoreType.Projectile) {
            ctx.workingSpell.projectile.prefabId = SpellProjectilePrefabId.ProjectileIce;
            return true;
        }

        if (ctx.workingSpell.coreType == CoreType.Zone) {
            ctx.workingSpell.zone.prefabId = SpellZonePrefabId.ZoneExplosionIce;
            return true;
        }

        return false;
    }
}