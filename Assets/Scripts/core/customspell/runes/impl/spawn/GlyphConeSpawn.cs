using UnityEngine;

[CreateAssetMenu(fileName = "ConeSpawn", menuName = "Glyphs/Spawn/ConeSpawn")]
public class GlyphConeSpawn : RuneGlyphDefinition {
    [Header("Properties")]
    public float coneRadius = 2f;
    public float coneHeight = 2f;

    public override bool Apply(RuneGlyphContext ctx) {
        ctx.workingSpell.spawn.spawnMode = SpawnMode.Cone;
        ctx.workingSpell.spawn.coneRadius = coneRadius;
        ctx.workingSpell.spawn.coneHeight = coneHeight;
        ctx.workingSpell.spawn.multiInstanceDelay = 0f;
        return true;
    }
}