using UnityEngine;

[CreateAssetMenu(fileName = "InstantSpawn", menuName = "Glyphs/Spawn/InstantSpawn")]
public class GlyphInstantSpawn : RuneGlyphDefinition {
    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.spawn.multiInstanceDelay = 0f;
        return true;
    }
}