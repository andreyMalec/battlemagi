using UnityEngine;

[CreateAssetMenu(fileName = "RainSpawn", menuName = "Glyphs/Spawn/RainSpawn")]
public class GlyphRainSpawn : RuneGlyphDefinition {
    [Header("Properties")]
    public float circleRadius = 2f;
    public float circleHeight = 2f;

    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.spawn.spawnMode = SpawnMode.GroundPointDiskUp;
        ctx.workingSpell.spawn.circleRadius = circleRadius;
        ctx.workingSpell.spawn.circleHeight = circleHeight;
        ctx.workingSpell.spawn.preview = Preview.Disk;
        return true;
    }
}