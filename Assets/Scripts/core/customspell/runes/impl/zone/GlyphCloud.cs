using UnityEngine;

/**
 * Облако
 * Область с периодическим уроном
 */
[CreateAssetMenu(fileName = "Cloud", menuName = "Glyphs/Zone/Cloud")]
public class GlyphCloud : RuneGlyphDefinition {
    [Header("Properties")]
    public float lifetime = 10f;
    
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Zone;
        var zone = CreateInstance<ZoneDefinition>();
        ctx.workingSpell.zone = zone;
        var dmg = CreateInstance<DamageDefinition>();
        dmg.canHitAllies = true;
        dmg.mode = SpellDamageMode.OverTime;
        ctx.workingSpell.damage = dmg;
        ctx.workingSpell.lifetime = lifetime;
        return true;
    }
}