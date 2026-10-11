using System.Collections.Generic;

public class RuneGlyphContext {
    public SpellDefinition baseSpell;
    public SpellDefinition workingSpell;
    public SpellDefinition nestedSpell;
    public RuneGlyphDefinition triggerGlyph;
    public RuneGlyphDefinition coreGlyph;
    public RunePlacedGlyph currentPlacedGlyph;
    public RuneGlyphDefinition currentGlyph;
    public IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId;
    public IReadOnlyDictionary<string, RunePlacedGlyph> placedById;
    public IReadOnlyDictionary<string, List<RuneLink>> outgoingLinks;
    public IReadOnlyDictionary<string, List<RuneLink>> incomingLinks;
    public readonly HashSet<RuneGlyphTag> activeTags = new HashSet<RuneGlyphTag>();
    public readonly List<string> appliedPlacements = new List<string>();
    public readonly List<string> errors = new List<string>();

    public void AddError(string error) {
        if (string.IsNullOrWhiteSpace(error))
            return;
        errors.Add(error);
    }

    public bool HasTag(RuneGlyphTag tag) {
        return activeTags.Contains(tag);
    }

    public void AddTag(RuneGlyphTag tag) {
        activeTags.Add(tag);
    }
}