using UnityEngine;

[CreateAssetMenu(fileName = "MasterCopy", menuName = "Glyphs/MasterCopy")]
public class GlyphMasterCopy : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.spawn.instanceCount += 15;
        return true;
    }
}