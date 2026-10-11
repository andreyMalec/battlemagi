using UnityEngine;

[CreateAssetMenu(fileName = "Copy", menuName = "Glyphs/Copy")]
public class GlyphCopy : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.spawn.instanceCount += 1;
        return true;
    }
}