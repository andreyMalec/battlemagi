using System.Collections.Generic;
using System.Linq;

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
    public readonly HashSet<SpellTag> activeTags = new();
    public readonly List<string> appliedPlacements = new();
    public readonly List<string> errors = new();
    public readonly List<float> damageMultipliers = new();

    public void AddError(string error) {
        if (string.IsNullOrWhiteSpace(error))
            return;
        errors.Add(error);
    }

    public bool HasTag(SpellTag tag) {
        return activeTags.Contains(tag);
    }

    public bool IsProjectile() {
        return activeTags.Any(tag => tag.name.ToLower().Equals("projectile"));
    }

    public bool IsArea() {
        return activeTags.Any(tag => tag.name.ToLower().Equals("area"));
    }

    public void AddTag(SpellTag tag) {
        activeTags.Add(tag);
    }

    public void AddDamageMultiplier(float multiplier) {
        damageMultipliers.Add(multiplier);
    }
}