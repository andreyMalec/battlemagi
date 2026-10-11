using UnityEngine;

/**
 * Стрела
 * Снаряд высокой скорости
 */
[CreateAssetMenu(fileName = "Bolt", menuName = "Glyphs/Projectile/Bolt")]
public class GlyphBolt : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Projectile;
        var proj = CreateInstance<ProjectileDefinition>();
        proj.moveSpeed = 120f;
        proj.moveType = SpellMovement.Linear;
        ctx.workingSpell.projectile = proj;
        return true;
    }
}