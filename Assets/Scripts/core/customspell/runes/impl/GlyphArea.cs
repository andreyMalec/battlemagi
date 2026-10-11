using UnityEngine;

[CreateAssetMenu(fileName = "Area", menuName = "Glyphs/Area")]
public class GlyphArea : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.scale += 1;
        ctx.workingSpell.spawn.circleRadius += 1;
        return true;
    }
}