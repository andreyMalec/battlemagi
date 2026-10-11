using UnityEngine;

[CreateAssetMenu(fileName = "Spiral", menuName = "Glyphs/Movement/Spiral")]
public class GlyphSpiral : RuneGlyphDefinition {
    [Header("Properties")]
    public float angularSpeed = 5;

    public float spiralRadius = 0.5f;

    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            var proj = ctx.workingSpell.projectile;
            proj.moveType = SpellMovement.Spiral;
            proj.angularSpeed = proj.moveSpeed * Mathf.PI  * 2 / angularSpeed;
            proj.spiralRadius = spiralRadius;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            var zone = ctx.workingSpell.zone;
            zone.moveType = SpellMovement.Spiral;
            zone.angularSpeed = zone.moveSpeed * Mathf.PI  * 2 / angularSpeed;
            zone.spiralRadius = spiralRadius;
            return true;
        }

        return false;
    }
}