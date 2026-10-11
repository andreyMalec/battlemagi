using UnityEngine;

/**
 * Импульс
 * Область с единорозовым уроном при касании
 */
[CreateAssetMenu(fileName = "Pulse", menuName = "Glyphs/Zone/Pulse")]
public class GlyphPulse : RuneGlyphDefinition {
    [Header("Properties")]
    public float lifetime = 5f;
    
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Zone;
        var zone = CreateInstance<ZoneDefinition>();
        ctx.workingSpell.zone = zone;
        var dmg = CreateInstance<DamageDefinition>();
        dmg.canHitAllies = true;
        dmg.mode = SpellDamageMode.OncePerTarget;
        ctx.workingSpell.damage = dmg;
        ctx.workingSpell.lifetime = lifetime;
        return true;
    }
}