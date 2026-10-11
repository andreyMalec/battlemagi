using UnityEngine;

[CreateAssetMenu(fileName = "PointSpawn", menuName = "Glyphs/Spawn/PointSpawn")]
public class GlyphPointSpawn : RuneGlyphDefinition {
    [Header("Properties")]
    public float raycastMaxDistance = 50f;

    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.spawn.spawnMode == SpawnMode.Arc)
            ctx.workingSpell.spawn.spawnMode = SpawnMode.GroundPointArc;
        else if (ctx.workingSpell.spawn.spawnMode == SpawnMode.DirectDownForward ||
                 ctx.workingSpell.spawn.spawnMode == SpawnMode.GroundPointForward)
            ctx.workingSpell.spawn.spawnMode = SpawnMode.GroundPointForward;
        else
            ctx.workingSpell.spawn.spawnMode = SpawnMode.GroundPoint;
        ctx.workingSpell.spawn.raycastMaxDistance = raycastMaxDistance;
        ctx.workingSpell.spawn.preview |= Preview.GroundPoint;
        return true;
    }
}