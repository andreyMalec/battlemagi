using UnityEngine;

[CreateAssetMenu(fileName = "FollowCaster", menuName = "Glyphs/Movement/FollowCaster")]
public class GlyphFollowCaster : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            var proj = ctx.workingSpell.projectile;
            proj.moveType = SpellMovement.FollowCaster;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            var zone = ctx.workingSpell.zone;
            zone.moveType = SpellMovement.FollowCaster;
            return true;
        }

        return false;
    }
}