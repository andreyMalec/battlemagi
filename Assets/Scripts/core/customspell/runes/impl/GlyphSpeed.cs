using UnityEngine;

[CreateAssetMenu(fileName = "Speed", menuName = "Glyphs/Speed")]
public class GlyphSpeed : RuneGlyphDefinition {
    [Header("Properties")]
    public float projSpeed = 60f;
    public float zoneSpeed = 20f;
    
    
    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            var proj = ctx.workingSpell.projectile;
            proj.moveSpeed += projSpeed;
            return true;
        }

        if (ctx.HasTag(RuneGlyphTag.Area)) {
            var zone = ctx.workingSpell.zone;
            zone.moveSpeed += zoneSpeed;
            return true;
        }

        return true;
    }
}