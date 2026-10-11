using System.Collections.Generic;

public class RuneSpellAssembler {
    private readonly Dictionary<string, RuneGlyphDefinition> _glyphCatalog;

    public RuneSpellAssembler(IEnumerable<RuneGlyphDefinition> glyphCatalog) {
        _glyphCatalog = new Dictionary<string, RuneGlyphDefinition>();
        foreach (var glyph in glyphCatalog) {
            if (string.IsNullOrWhiteSpace(glyph.glyphId))
                continue;
            _glyphCatalog[glyph.glyphId] = glyph;
        }
    }

    public bool TryAssemble(RuneBoardData board, SpellDefinition baseTemplate, out SpellDefinition spell, out string error) {
        spell = null;
        error = string.Empty;

        if (board == null) {
            error = "Rune board is null.";
            return false;
        }

        if (baseTemplate == null) {
            error = "Base spell template is not assigned.";
            return false;
        }

        if (board.glyphs == null || board.glyphs.Count == 0) {
            error = "Rune board is empty.";
            return false;
        }

        var placedById = BuildPlacedMap(board);
        var glyphByPlacementId = ResolveGlyphs(placedById, out error);
        if (glyphByPlacementId == null)
            return false;

        var outgoing = BuildOutgoing(board, placedById);
        var incoming = BuildIncoming(board, placedById);

        var triggerId = SelectTrigger(glyphByPlacementId, outgoing);
        var coreId = SelectCore(glyphByPlacementId, triggerId, outgoing);
        if (string.IsNullOrWhiteSpace(coreId)) {
            error = "Core glyph was not found.";
            return false;
        }

        var activeBranches = new HashSet<string>();
        return TryAssembleBranch(
            coreId,
            triggerId,
            baseTemplate,
            placedById,
            glyphByPlacementId,
            outgoing,
            incoming,
            activeBranches,
            out spell,
            out error
        );
    }

    public static SpellDefinition CloneSpell(SpellDefinition source) {
        var json = SpellJsonSerializer.ToJson(source);
        return SpellJsonSerializer.FromJson<SpellDefinition>(json);
    }

    private static Dictionary<string, RunePlacedGlyph> BuildPlacedMap(RuneBoardData board) {
        var map = new Dictionary<string, RunePlacedGlyph>(board.glyphs.Count);
        for (var i = 0; i < board.glyphs.Count; i++) {
            var placed = board.glyphs[i];
            if (string.IsNullOrWhiteSpace(placed.placementId))
                continue;
            map[placed.placementId] = placed;
        }

        return map;
    }

    private Dictionary<string, RuneGlyphDefinition> ResolveGlyphs(Dictionary<string, RunePlacedGlyph> placedById, out string error) {
        error = string.Empty;
        var map = new Dictionary<string, RuneGlyphDefinition>(placedById.Count);

        foreach (var pair in placedById) {
            var glyphId = pair.Value.glyphId;
            if (!_glyphCatalog.TryGetValue(glyphId, out var glyph)) {
                error = $"Glyph is missing in catalog: {glyphId}";
                return null;
            }

            map[pair.Key] = glyph;
        }

        return map;
    }

    private static Dictionary<string, List<RuneLink>> BuildOutgoing(RuneBoardData board, Dictionary<string, RunePlacedGlyph> placedById) {
        var map = new Dictionary<string, List<RuneLink>>(placedById.Count);
        foreach (var placementId in placedById.Keys) {
            map[placementId] = new List<RuneLink>();
        }

        if (board.links == null)
            return map;

        for (var i = 0; i < board.links.Count; i++) {
            var link = board.links[i];
            if (!placedById.ContainsKey(link.fromPlacementId) || !placedById.ContainsKey(link.toPlacementId))
                continue;
            map[link.fromPlacementId].Add(link);
        }

        return map;
    }

    private static Dictionary<string, List<RuneLink>> BuildIncoming(RuneBoardData board, Dictionary<string, RunePlacedGlyph> placedById) {
        var map = new Dictionary<string, List<RuneLink>>(placedById.Count);
        foreach (var placementId in placedById.Keys) {
            map[placementId] = new List<RuneLink>();
        }

        if (board.links == null)
            return map;

        for (var i = 0; i < board.links.Count; i++) {
            var link = board.links[i];
            if (!placedById.ContainsKey(link.fromPlacementId) || !placedById.ContainsKey(link.toPlacementId))
                continue;
            map[link.toPlacementId].Add(link);
        }

        return map;
    }

    private static string SelectTrigger(IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId, IReadOnlyDictionary<string, List<RuneLink>> outgoing) {
        var bestId = string.Empty;
        var bestScore = int.MinValue;

        foreach (var pair in glyphByPlacementId) {
            if (pair.Value.glyphType != RuneGlyphType.Trigger)
                continue;

            var score = 0;
            if (outgoing.TryGetValue(pair.Key, out var links))
                score += links.Count;
            if (score > bestScore || (score == bestScore && string.CompareOrdinal(pair.Key, bestId) < 0)) {
                bestScore = score;
                bestId = pair.Key;
            }
        }

        return bestId;
    }

    private static string SelectCore(IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId, string triggerId, IReadOnlyDictionary<string, List<RuneLink>> outgoing) {
        if (!string.IsNullOrWhiteSpace(triggerId) && outgoing.TryGetValue(triggerId, out var triggerLinks)) {
            for (var i = 0; i < triggerLinks.Count; i++) {
                var targetId = triggerLinks[i].toPlacementId;
                if (!glyphByPlacementId.TryGetValue(targetId, out var target))
                    continue;
                if (target.glyphType == RuneGlyphType.Core)
                    return targetId;
            }
        }

        var bestId = string.Empty;
        foreach (var pair in glyphByPlacementId) {
            if (pair.Value.glyphType != RuneGlyphType.Core)
                continue;
            if (string.IsNullOrWhiteSpace(bestId) || string.CompareOrdinal(pair.Key, bestId) < 0)
                bestId = pair.Key;
        }

        return bestId;
    }

    private static string SelectCoreIncoming(IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId, string triggerId, IReadOnlyDictionary<string, List<RuneLink>> incoming) {
        if (!string.IsNullOrWhiteSpace(triggerId) && incoming.TryGetValue(triggerId, out var triggerLinks)) {
            for (var i = 0; i < triggerLinks.Count; i++) {
                var targetId = triggerLinks[i].fromPlacementId;
                if (!glyphByPlacementId.TryGetValue(targetId, out var target))
                    continue;
                if (target.glyphType == RuneGlyphType.Core)
                    return targetId;
            }
        }

        var bestId = string.Empty;
        foreach (var pair in glyphByPlacementId) {
            if (pair.Value.glyphType != RuneGlyphType.Core)
                continue;
            if (string.IsNullOrWhiteSpace(bestId) || string.CompareOrdinal(pair.Key, bestId) < 0)
                bestId = pair.Key;
        }

        return bestId;
    }

    private bool TryAssembleBranch(
        string coreId,
        string sourceTriggerId,
        SpellDefinition baseTemplate,
        IReadOnlyDictionary<string, RunePlacedGlyph> placedById,
        IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId,
        IReadOnlyDictionary<string, List<RuneLink>> outgoing,
        IReadOnlyDictionary<string, List<RuneLink>> incoming,
        HashSet<string> activeBranches,
        out SpellDefinition spell,
        out string error
    ) {
        spell = null;
        error = string.Empty;

        var branchKey = string.IsNullOrWhiteSpace(sourceTriggerId)
            ? $"root:{coreId}"
            : $"{sourceTriggerId}->{coreId}";
        if (!activeBranches.Add(branchKey)) {
            error = $"Recursive trigger loop detected at branch {branchKey}.";
            return false;
        }

        try {
            var executionOrder = BuildExecutionOrder(coreId, glyphByPlacementId, outgoing, incoming);
            if (executionOrder.Count == 0) {
                error = "No glyph chain to assemble.";
                return false;
            }

            spell = CloneSpell(baseTemplate);
            var context = new RuneGlyphContext {
                baseSpell = baseTemplate,
                workingSpell = spell,
                glyphByPlacementId = glyphByPlacementId,
                placedById = placedById,
                outgoingLinks = outgoing,
                incomingLinks = incoming,
                triggerGlyph = !string.IsNullOrWhiteSpace(sourceTriggerId) && glyphByPlacementId.ContainsKey(sourceTriggerId)
                    ? glyphByPlacementId[sourceTriggerId]
                    : null,
                coreGlyph = glyphByPlacementId[coreId],
            };

            for (var i = 0; i < executionOrder.Count; i++) {
                var placementId = executionOrder[i];
                var glyph = glyphByPlacementId[placementId];
                context.currentPlacedGlyph = placedById[placementId];
                context.currentGlyph = glyph;
                context.nestedSpell = null;
                if (glyph.glyphType == RuneGlyphType.Core)
                    context.coreGlyph = glyph;

                if (!ValidateGlyphTags(context, glyph)) {
                    error = $"Glyph tag requirements failed: {glyph.glyphId}";
                    error += "\n" + string.Join("\n", context.errors);
                    return false;
                }

                if (!glyph.Apply(context)) {
                    error = $"Glyph apply failed: {glyph.glyphId}";
                    error += "\n" + string.Join("\n", context.errors);
                    return false;
                }

                AddGlyphTags(context, glyph);

                context.appliedPlacements.Add(placementId);
            }

            if (context.coreGlyph == null) {
                error = "Core glyph was not applied.";
                return false;
            }

            if (context.errors.Count > 0) {
                error = context.errors[0];
                return false;
            }

            var childTriggers = CollectChildTriggers(executionOrder, glyphByPlacementId, incoming);
            for (var i = 0; i < childTriggers.Count; i++) {
                var childTriggerId = childTriggers[i];
                var childCoreId = SelectCoreIncoming(glyphByPlacementId, childTriggerId, incoming);
                if (string.IsNullOrWhiteSpace(childCoreId)) {
                    error = $"Trigger has no core target: {childTriggerId}";
                    return false;
                }

                if (!TryAssembleBranch(
                        childCoreId,
                        childTriggerId,
                        baseTemplate,
                        placedById,
                        glyphByPlacementId,
                        outgoing,
                        incoming,
                        activeBranches,
                        out var nestedSpell,
                        out error
                    )) {
                    return false;
                }

                var triggerGlyph = glyphByPlacementId[childTriggerId];
                context.currentPlacedGlyph = placedById[childTriggerId];
                context.currentGlyph = triggerGlyph;
                context.triggerGlyph = triggerGlyph;
                context.nestedSpell = nestedSpell;

                if (!ValidateGlyphTags(context, triggerGlyph)) {
                    error = $"Trigger tag requirements failed: {triggerGlyph.glyphId}";
                    error += "\n" + string.Join("\n", context.errors);
                    return false;
                }

                if (!triggerGlyph.Apply(context)) {
                    error = $"Trigger apply failed: {triggerGlyph.glyphId}";
                    return false;
                }

                AddGlyphTags(context, triggerGlyph);

                context.appliedPlacements.Add(childTriggerId);
            }

            spell.Validate();
            if (!HasValidCorePayload(spell)) {
                error = "Compiled spell has invalid core payload.";
                return false;
            }

            return true;
        }
        finally {
            activeBranches.Remove(branchKey);
        }
    }

    private static List<string> BuildExecutionOrder(string coreId,
        IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId,
        IReadOnlyDictionary<string, List<RuneLink>> outgoing,
        IReadOnlyDictionary<string, List<RuneLink>> incoming) {
        var sequence = new List<string>(glyphByPlacementId.Count);
        if (string.IsNullOrWhiteSpace(coreId) || !glyphByPlacementId.ContainsKey(coreId))
            return sequence;

        var reachable = CollectReachableWithoutTriggers(coreId, glyphByPlacementId, outgoing);
        var reachable2 = CollectReachableWithoutTriggersIncoming(coreId, glyphByPlacementId, incoming);
        for (var i = 0; i < reachable2.Count; i++) {
            var node = reachable2[i];
            if (!glyphByPlacementId.ContainsKey(node))
                continue;
            if (glyphByPlacementId[node].glyphType == RuneGlyphType.Trigger)
                continue;
            sequence.Add(node);
        }

        sequence.Sort((left, right) => {
            var leftType = (int)glyphByPlacementId[left].glyphType;
            var rightType = (int)glyphByPlacementId[right].glyphType;
            if (leftType != rightType)
                return leftType.CompareTo(rightType);
            return 0;
        });

        return sequence;
    }

    private static List<string> CollectReachableWithoutTriggers(
        string startId,
        IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId,
        IReadOnlyDictionary<string, List<RuneLink>> outgoing
    ) {
        var ordered = new List<string>();
        if (string.IsNullOrWhiteSpace(startId) || !outgoing.ContainsKey(startId))
            return ordered;

        var queue = new Queue<string>();
        var seen = new HashSet<string> { startId };
        queue.Enqueue(startId);

        while (queue.Count > 0) {
            var current = queue.Dequeue();
            ordered.Add(current);
            var links = outgoing[current];
            for (var i = 0; i < links.Count; i++) {
                var next = links[i].toPlacementId;
                if (string.IsNullOrWhiteSpace(next))
                    continue;
                if (!outgoing.ContainsKey(next))
                    continue;
                if (!glyphByPlacementId.ContainsKey(next))
                    continue;
                if (glyphByPlacementId[next].glyphType == RuneGlyphType.Trigger)
                    continue;
                if (!seen.Add(next))
                    continue;
                queue.Enqueue(next);
            }
        }

        return ordered;
    }

    private static List<string> CollectReachableWithoutTriggersIncoming(
        string startId,
        IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId,
        IReadOnlyDictionary<string, List<RuneLink>> incoming
    ) {
        var ordered = new List<string>();
        if (string.IsNullOrWhiteSpace(startId) || !incoming.ContainsKey(startId))
            return ordered;

        var queue = new Queue<string>();
        var seen = new HashSet<string> { startId };
        queue.Enqueue(startId);

        while (queue.Count > 0) {
            var current = queue.Dequeue();
            ordered.Add(current);
            var links = incoming[current];
            for (var i = 0; i < links.Count; i++) {
                var next = links[i].fromPlacementId;
                if (string.IsNullOrWhiteSpace(next))
                    continue;
                if (!incoming.ContainsKey(next))
                    continue;
                if (!glyphByPlacementId.ContainsKey(next))
                    continue;
                if (glyphByPlacementId[next].glyphType == RuneGlyphType.Trigger)
                    continue;
                if (!seen.Add(next))
                    continue;
                queue.Enqueue(next);
            }
        }

        return ordered;
    }

    private static List<string> CollectChildTriggers(
        IReadOnlyList<string> parentExecutionOrder,
        IReadOnlyDictionary<string, RuneGlyphDefinition> glyphByPlacementId,
        IReadOnlyDictionary<string, List<RuneLink>> incoming
    ) {
        var list = new List<string>();
        var added = new HashSet<string>();

        for (var i = 0; i < parentExecutionOrder.Count; i++) {
            var sourceId = parentExecutionOrder[i];
            if (!incoming.TryGetValue(sourceId, out var links))
                continue;

            for (var l = 0; l < links.Count; l++) {
                var triggerId = links[l].fromPlacementId;
                if (!glyphByPlacementId.TryGetValue(triggerId, out var triggerGlyph))
                    continue;
                if (triggerGlyph.glyphType != RuneGlyphType.Trigger)
                    continue;
                if (added.Add(triggerId))
                    list.Add(triggerId);
            }
        }

        list.Sort(string.CompareOrdinal);
        return list;
    }

    private static bool HasValidCorePayload(SpellDefinition spell) {
        if (spell == null)
            return false;

        switch (spell.coreType) {
            case CoreType.Projectile:
                return spell.projectile != null;
            case CoreType.Zone:
                return spell.zone != null;
            case CoreType.Beam:
                return spell.beam != null;
            case CoreType.Summon:
                return spell.summon != null;
            case CoreType.Self:
                return spell.self != null;
            default:
                return false;
        }
    }

    private static bool ValidateGlyphTags(RuneGlyphContext context, RuneGlyphDefinition glyph) {
        var hasError = false;
        for (var i = 0; i < glyph.requiredTags.Count; i++) {
            var required = glyph.requiredTags[i];
            if (context.HasTag(required))
                continue;
            context.AddError($"Glyph {glyph.glyphId} requires tag '{required}'.");
            hasError = true;
        }

        for (var i = 0; i < glyph.forbiddenTags.Count; i++) {
            var forbidden = glyph.forbiddenTags[i];
            if (!context.HasTag(forbidden))
                continue;
            context.AddError($"Glyph {glyph.glyphId} is forbidden with tag '{forbidden}'.");
            hasError = true;
        }

        return !hasError;
    }

    private static void AddGlyphTags(RuneGlyphContext context, RuneGlyphDefinition glyph) {
        var tags = glyph.glyphTags;
        for (var i = 0; i < tags.Count; i++) {
            context.AddTag(tags[i]);
        }
    }
}


