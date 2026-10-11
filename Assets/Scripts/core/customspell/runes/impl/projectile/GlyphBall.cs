using UnityEngine;

/**
 * Шар
 * Снаряд средней скорости с гравитацией
 */
[CreateAssetMenu(fileName = "Ball", menuName = "Glyphs/Projectile/Ball")]
public class GlyphBall : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Projectile;
        var proj = CreateInstance<ProjectileDefinition>();
        proj.moveSpeed = 60f;
        proj.enableGravity = true;
        proj.gravity = new Vector3(0, -9.81f, 0);
        proj.moveType = SpellMovement.Linear;
        ctx.workingSpell.projectile = proj;
        return true;
    }
}