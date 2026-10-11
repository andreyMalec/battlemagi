using UnityEngine;

[CreateAssetMenu(fileName = "ForwardSpawn", menuName = "Glyphs/Spawn/ForwardSpawn")]
public class GlyphForwardSpawn : RuneGlyphDefinition {
    [Header("Properties")]
    public float raycastMaxDistance = 50f;

    public float forwardStep = 3f;

    public override bool Apply(RuneGlyphContext ctx) {
        if (ctx.workingSpell.spawn.spawnMode == SpawnMode.GroundPoint ||
            ctx.workingSpell.spawn.spawnMode == SpawnMode.GroundPointForward)
            ctx.workingSpell.spawn.spawnMode = SpawnMode.GroundPointForward;
        else
            ctx.workingSpell.spawn.spawnMode = SpawnMode.DirectDownForward;
        ctx.workingSpell.spawn.raycastMaxDistance = raycastMaxDistance;
        ctx.workingSpell.spawn.forwardStep += forwardStep;
        ctx.workingSpell.spawn.preview |= Preview.GroundPoint;
        ctx.workingSpell.spawn.preview |= Preview.Mesh;
        return true;
    }
}