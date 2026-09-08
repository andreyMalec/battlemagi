using System;
using System.Collections.Generic;

public class RuneBoardService {
    private static readonly HexCoord[] SideDirections = {
        new HexCoord(1, 0),
        new HexCoord(1, -1),
        new HexCoord(0, -1),
        new HexCoord(-1, 0),
        new HexCoord(-1, 1),
        new HexCoord(0, 1),
    };

    private readonly Dictionary<string, RuneGlyphDefinition> _glyphCatalog;

    public RuneBoardService(IEnumerable<RuneGlyphDefinition> glyphCatalog) {
        _glyphCatalog = new Dictionary<string, RuneGlyphDefinition>();
        foreach (var glyph in glyphCatalog) {
            if (string.IsNullOrWhiteSpace(glyph.glyphId))
                continue;
            _glyphCatalog[glyph.glyphId] = glyph;
        }
    }

    public bool TryPlaceGlyph(RuneBoardData board, string glyphId, HexCoord anchor, int rotation, out string placementId) {
        placementId = string.Empty;
        if (!_glyphCatalog.ContainsKey(glyphId))
            return false;

        var candidate = new RunePlacedGlyph {
            placementId = Guid.NewGuid().ToString("N"),
            glyphId = glyphId,
            anchor = anchor,
            rotation = NormalizeRotation(rotation),
        };

        if (!CanPlace(board, candidate))
            return false;

        board.glyphs.Add(candidate);
        placementId = candidate.placementId;
        return true;
    }

    public bool TryMoveGlyph(RuneBoardData board, string placementId, HexCoord delta) {
        var glyph = GetPlacedGlyph(board, placementId);
        if (glyph == null)
            return false;

        var snapshot = glyph.anchor;
        glyph.anchor = glyph.anchor + delta;
        if (CanPlace(board, glyph, placementId))
            return true;

        glyph.anchor = snapshot;
        return false;
    }

    public bool TryRotateGlyph(RuneBoardData board, string placementId, int clockwiseSteps) {
        var glyph = GetPlacedGlyph(board, placementId);
        if (glyph == null)
            return false;

        var snapshot = glyph.rotation;
        glyph.rotation = NormalizeRotation(glyph.rotation + clockwiseSteps);
        if (CanPlace(board, glyph, placementId))
            return true;

        glyph.rotation = snapshot;
        return false;
    }

    public bool RemoveGlyph(RuneBoardData board, string placementId) {
        var index = board.glyphs.FindIndex(g => g.placementId == placementId);
        if (index < 0)
            return false;

        board.glyphs.RemoveAt(index);
        board.links.RemoveAll(l => l.fromPlacementId == placementId || l.toPlacementId == placementId);
        return true;
    }

    public bool TryLink(RuneBoardData board, string fromPlacementId, string toPlacementId, int fromSide, int toSide) {
        if (fromPlacementId == toPlacementId)
            return false;

        var from = GetPlacedGlyph(board, fromPlacementId);
        var to = GetPlacedGlyph(board, toPlacementId);
        if (from == null || to == null)
            return false;

        if (!AreCompatible(from.glyphId, to.glyphId))
            return false;

        if (!CanTouchOnSides(from, to, fromSide, toSide))
            return false;

        if (board.links.Exists(l => l.fromPlacementId == fromPlacementId && l.toPlacementId == toPlacementId))
            return false;

        var link = new RuneLink {
            fromPlacementId = fromPlacementId,
            toPlacementId = toPlacementId,
            fromSide = NormalizeSide(fromSide),
            toSide = NormalizeSide(toSide),
        };

        board.links.Add(link);
        if (HasCycle(board)) {
            board.links.Remove(link);
            return false;
        }

        return true;
    }

    public bool RemoveLink(RuneBoardData board, string fromPlacementId, string toPlacementId) {
        var index = board.links.FindIndex(l => l.fromPlacementId == fromPlacementId && l.toPlacementId == toPlacementId);
        if (index < 0)
            return false;

        board.links.RemoveAt(index);
        return true;
    }

    public bool TryMoveGroup(RuneBoardData board, IReadOnlyList<string> placementIds, HexCoord delta) {
        var moved = new List<(RunePlacedGlyph glyph, HexCoord original)>();
        for (var i = 0; i < placementIds.Count; i++) {
            var glyph = GetPlacedGlyph(board, placementIds[i]);
            if (glyph == null)
                continue;
            moved.Add((glyph, glyph.anchor));
            glyph.anchor = glyph.anchor + delta;
        }

        for (var i = 0; i < moved.Count; i++) {
            if (!CanPlace(board, moved[i].glyph, moved[i].glyph.placementId)) {
                for (var j = 0; j < moved.Count; j++) {
                    moved[j].glyph.anchor = moved[j].original;
                }

                return false;
            }
        }

        return true;
    }

    public bool TryRotateGroup(RuneBoardData board, IReadOnlyList<string> placementIds, HexCoord pivot, int clockwiseSteps) {
        var rotated = new List<(RunePlacedGlyph glyph, HexCoord originalAnchor, int originalRotation)>();
        for (var i = 0; i < placementIds.Count; i++) {
            var glyph = GetPlacedGlyph(board, placementIds[i]);
            if (glyph == null)
                continue;

            rotated.Add((glyph, glyph.anchor, glyph.rotation));
            var relative = new HexCoord(glyph.anchor.q - pivot.q, glyph.anchor.r - pivot.r);
            var rotatedRelative = HexCoord.Rotate(relative, clockwiseSteps);
            glyph.anchor = pivot + rotatedRelative;
            glyph.rotation = NormalizeRotation(glyph.rotation + clockwiseSteps);
        }

        for (var i = 0; i < rotated.Count; i++) {
            if (!CanPlace(board, rotated[i].glyph, rotated[i].glyph.placementId)) {
                for (var j = 0; j < rotated.Count; j++) {
                    rotated[j].glyph.anchor = rotated[j].originalAnchor;
                    rotated[j].glyph.rotation = rotated[j].originalRotation;
                }

                return false;
            }
        }

        return true;
    }

    public void RebuildAutoLinks(RuneBoardData board) {
        board.links.Clear();

        var occupied = new Dictionary<HexCoord, string>();
        var byPlacement = new Dictionary<string, RunePlacedGlyph>();
        for (var i = 0; i < board.glyphs.Count; i++) {
            var glyph = board.glyphs[i];
            byPlacement[glyph.placementId] = glyph;
            foreach (var cell in OccupiedCells(glyph)) {
                occupied[cell] = glyph.placementId;
            }
        }

        for (var i = 0; i < board.glyphs.Count; i++) {
            var from = board.glyphs[i];
            if (!_glyphCatalog.TryGetValue(from.glyphId, out var fromDef))
                continue;

            var outputLocal = fromDef.shape.Count > 0 ? fromDef.shape[0].ToCoord() : new HexCoord(0, 0);
            var outputCell = from.anchor + HexCoord.Rotate(outputLocal, from.rotation);
            var fromSide = NormalizeSide(fromDef.outputSide - from.rotation);
            var targetCell = outputCell + SideDirections[fromSide];

            if (!occupied.TryGetValue(targetCell, out var targetPlacementId))
                continue;

            if (targetPlacementId == from.placementId)
                continue;

            if (!byPlacement.TryGetValue(targetPlacementId, out var to))
                continue;

            if (!AreCompatible(from.glyphId, to.glyphId))
                continue;

            var link = new RuneLink {
                fromPlacementId = from.placementId,
                toPlacementId = targetPlacementId,
                fromSide = fromSide,
                toSide = NormalizeSide(fromSide + 3)
            };

            board.links.Add(link);
            if (HasCycle(board)) {
                board.links.RemoveAt(board.links.Count - 1);
            }
        }
    }

    public bool HasCycle(RuneBoardData board) {
        var state = new Dictionary<string, int>();
        for (var i = 0; i < board.glyphs.Count; i++) {
            state[board.glyphs[i].placementId] = 0;
        }

        for (var i = 0; i < board.glyphs.Count; i++) {
            var id = board.glyphs[i].placementId;
            if (state[id] == 0 && DfsHasCycle(board, id, state))
                return true;
        }

        return false;
    }

    public IEnumerable<HexCoord> OccupiedCells(RunePlacedGlyph glyph) {
        var def = _glyphCatalog[glyph.glyphId];
        var shape = def.GetLocalShape();
        for (var i = 0; i < shape.Count; i++) {
            var rotated = HexCoord.Rotate(shape[i], glyph.rotation);
            yield return glyph.anchor + rotated;
        }
    }

    private bool CanPlace(RuneBoardData board, RunePlacedGlyph candidate, string ignoredPlacementId = null) {
        var occupied = new HashSet<HexCoord>();
        for (var i = 0; i < board.glyphs.Count; i++) {
            var existing = board.glyphs[i];
            if (existing.placementId == ignoredPlacementId)
                continue;

            foreach (var cell in OccupiedCells(existing)) {
                occupied.Add(cell);
            }
        }

        foreach (var cell in OccupiedCells(candidate)) {
            if (!IsInsideRadius(cell, board.radius))
                return false;
            if (occupied.Contains(cell))
                return false;
        }

        return true;
    }

    private bool CanTouchOnSides(RunePlacedGlyph from, RunePlacedGlyph to, int fromSide, int toSide) {
        var fromDirection = SideDirections[NormalizeSide(fromSide)];
        var toDirection = SideDirections[NormalizeSide(toSide)];

        var fromCells = new HashSet<HexCoord>();
        foreach (var cell in OccupiedCells(from)) {
            fromCells.Add(cell);
        }

        var toCells = new HashSet<HexCoord>();
        foreach (var cell in OccupiedCells(to)) {
            toCells.Add(cell);
        }

        foreach (var fromCell in fromCells) {
            var neighbor = fromCell + fromDirection;
            if (!toCells.Contains(neighbor))
                continue;

            var back = neighbor + toDirection;
            if (back.Equals(fromCell))
                return true;
        }

        return false;
    }

    private RunePlacedGlyph GetPlacedGlyph(RuneBoardData board, string placementId) {
        return board.glyphs.Find(g => g.placementId == placementId);
    }

    private bool AreCompatible(string leftGlyphId, string rightGlyphId) {
        var left = _glyphCatalog[leftGlyphId];
        var right = _glyphCatalog[rightGlyphId];
        if (left.incompatibleGlyphIds.Contains(rightGlyphId))
            return false;
        if (right.incompatibleGlyphIds.Contains(leftGlyphId))
            return false;
        return true;
    }

    private bool DfsHasCycle(RuneBoardData board, string nodeId, Dictionary<string, int> state) {
        state[nodeId] = 1;
        for (var i = 0; i < board.links.Count; i++) {
            var link = board.links[i];
            if (link.fromPlacementId != nodeId)
                continue;

            if (!state.ContainsKey(link.toPlacementId))
                continue;

            var targetState = state[link.toPlacementId];
            if (targetState == 1)
                return true;
            if (targetState == 0 && DfsHasCycle(board, link.toPlacementId, state))
                return true;
        }

        state[nodeId] = 2;
        return false;
    }

    private static bool IsInsideRadius(HexCoord coord, int radius) {
        var aq = Math.Abs(coord.q);
        var ar = Math.Abs(coord.r);
        var @as = Math.Abs(coord.s);
        return Math.Max(aq, Math.Max(ar, @as)) <= radius;
    }

    private static int NormalizeRotation(int rotation) {
        return ((rotation % 6) + 6) % 6;
    }

    private static int NormalizeSide(int side) {
        return ((side % 6) + 6) % 6;
    }
}

