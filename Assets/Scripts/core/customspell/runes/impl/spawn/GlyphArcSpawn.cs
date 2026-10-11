using UnityEngine;

[CreateAssetMenu(fileName = "ArcSpawn", menuName = "Glyphs/Spawn/ArcSpawn")]
public class GlyphArcSpawn : RuneGlyphDefinition {
    [Header("Properties")]
    public float arcAngleStep = 15f;

    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.spawn.spawnMode == SpawnMode.GroundPoint)
            ctx.workingSpell.spawn.spawnMode = SpawnMode.GroundPointArc;
        else
            ctx.workingSpell.spawn.spawnMode = SpawnMode.Arc;
        ctx.workingSpell.spawn.arcAngleStep = arcAngleStep;
        ctx.workingSpell.spawn.multiInstanceDelay = 0f;
        ctx.workingSpell.spawn.preview |= Preview.Mesh;
        return true;
    }
}