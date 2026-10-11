using UnityEngine;

[CreateAssetMenu(fileName = "Echo", menuName = "Glyphs/Echo")]
public class GlyphEcho : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.echoCount += 1;
        return true;
    }
}