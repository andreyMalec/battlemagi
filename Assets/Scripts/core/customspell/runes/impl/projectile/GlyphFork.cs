using UnityEngine;

[CreateAssetMenu(fileName = "Fork", menuName = "Glyphs/Projectile/Fork")]
public class GlyphFork : RuneGlyphDefinition {
    [Header("Properties")]
    public float forkSpreadAngle = 35f;

    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.HasTag(RuneGlyphTag.Projectile)) {
            ctx.workingSpell.projectile.enableFork = true;
            ctx.workingSpell.projectile.forkCount += 1;
            ctx.workingSpell.projectile.forkSpreadAngle = forkSpreadAngle;
            return true;
        }

        return false;
    }
}