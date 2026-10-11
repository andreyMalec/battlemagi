using UnityEngine;

/**
 * Сгусток
 * Снаряд низкой скорости с высокой гравитацией
 */
[CreateAssetMenu(fileName = "Orb", menuName = "Glyphs/Projectile/Orb")]
public class GlyphOrb : RuneGlyphDefinition  {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Projectile;
        var proj = CreateInstance<ProjectileDefinition>();
        proj.moveSpeed = 20f;
        proj.enableGravity = true;
        proj.gravity = new Vector3(0, -18, 0);
        proj.moveType = SpellMovement.Linear;
        ctx.workingSpell.projectile = proj;
        return true;
    }
}