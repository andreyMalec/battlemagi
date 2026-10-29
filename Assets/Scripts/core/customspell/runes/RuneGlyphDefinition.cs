using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class RuneGlyphDefinition : ScriptableObject {
    public string glyphId;
    public string displayName;
    public RuneGlyphType glyphType;
    [Range(0, 5)] public int outputSide;
    [Min(0f)] public float baseCost = 1f;
    [Min(0f)] public float repeatCostPercent = 0f;
    public List<RuneGlyphTag> glyphTags = new List<RuneGlyphTag>();
    public List<RuneGlyphTag> requiredTags = new List<RuneGlyphTag>();
    public List<RuneGlyphTag> forbiddenTags = new List<RuneGlyphTag>();
    public List<RuneShapeCell> shape = new List<RuneShapeCell> { new RuneShapeCell(0, 0) };

    public IReadOnlyList<HexCoord> GetLocalShape() {
        var cells = new List<HexCoord>(shape.Count);
        for (var i = 0; i < shape.Count; i++) {
            cells.Add(shape[i].ToCoord());
        }

        return cells;
    }

    public abstract bool Apply(RuneGlyphContext ctx);

#if UNITY_EDITOR
    private void OnValidate() {
        outputSide = ((outputSide % 6) + 6) % 6;
        if (baseCost < 0f)
            baseCost = 0f;
        if (repeatCostPercent < 0f)
            repeatCostPercent = 0f;
    }
#endif
}