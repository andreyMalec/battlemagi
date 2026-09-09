using UnityEngine;

[CreateAssetMenu(fileName = "Explosion", menuName = "Glyphs/Explosion")]
public class GlyphExplosion : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.coreType = CoreType.Zone;
        var zone = CreateInstance<ZoneDefinition>();
        ctx.workingSpell.zone = zone;
        return true;
    }
}