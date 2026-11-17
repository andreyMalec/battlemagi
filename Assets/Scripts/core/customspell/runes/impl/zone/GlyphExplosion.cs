using UnityEngine;

/**
 * Взрыв
 * Область с уроном при появлении
 */
[CreateAssetMenu(fileName = "Explosion", menuName = "Glyphs/Zone/Explosion")]
public class GlyphExplosion : RuneGlyphDefinition {
    [Header("Properties")]
    public float lifetime = 2f;

    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Zone;
        ctx.workingSpell.lifetime = lifetime;

        var zone = CreateInstance<ZoneDefinition>();
        ctx.workingSpell.zone = zone;

        var dmg = CreateInstance<DamageDefinition>();
        dmg.canHitAllies = true;
        dmg.mode = SpellDamageMode.Instant;
        ctx.workingSpell.damage = dmg;

        return true;
    }
}