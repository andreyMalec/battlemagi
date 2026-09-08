using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "RuneGlyph", menuName = "Spells/Runes/Glyph")]
public class RuneGlyphDefinition : ScriptableObject {
    public string glyphId;
    public string displayName;
    public RuneGlyphType glyphType;
    [Range(0, 5)] public int outputSide;
    public List<RuneGlyphDefinition> incompatibleGlyphs = new List<RuneGlyphDefinition>();
    [HideInInspector] public List<string> incompatibleGlyphIds = new List<string>();
    public List<RuneShapeCell> shape = new List<RuneShapeCell> { new RuneShapeCell(0, 0) };

    public IReadOnlyList<HexCoord> GetLocalShape() {
        var cells = new List<HexCoord>(shape.Count);
        for (var i = 0; i < shape.Count; i++) {
            cells.Add(shape[i].ToCoord());
        }

        return cells;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        outputSide = ((outputSide % 6) + 6) % 6;
        incompatibleGlyphIds = incompatibleGlyphs.Select(item => item.glyphId).ToList();
    }
#endif
}