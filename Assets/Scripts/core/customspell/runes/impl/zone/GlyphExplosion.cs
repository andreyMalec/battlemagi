using UnityEngine;

/**
 * Взрыв
 * Область с уроном при появлении
 */
[CreateAssetMenu(fileName = "Explosion", menuName = "Glyphs/Explosion")]
public class GlyphExplosion : RuneGlyphDefinition {
    [Header("Properties")]
    public float lifetime = 2f;
    
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Zone;
        var zone = CreateInstance<ZoneDefinition>();
        ctx.workingSpell.zone = zone;
        ctx.workingSpell.lifetime = lifetime;
        return true;
    }
}