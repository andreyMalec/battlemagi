using UnityEngine;

[CreateAssetMenu(fileName = "Ball", menuName = "Glyphs/Ball")]
public class GlyphBall : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Projectile;
        var proj = CreateInstance<ProjectileDefinition>();
        ctx.workingSpell.projectile = proj;
        proj.moveSpeed = 60f;
        proj.gravity = new Vector3(0, -9.81f, 0);
        proj.moveType = SpellMovement.Linear;
        return true;
    }
}