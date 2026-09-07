using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct HexCoord : IEquatable<HexCoord> {
    public int q;
    public int r;

    public int s => -q - r;

    public HexCoord(int q, int r) {
        this.q = q;
        this.r = r;
    }

    public static HexCoord operator +(HexCoord a, HexCoord b) {
        return new HexCoord(a.q + b.q, a.r + b.r);
    }

    public static HexCoord RotateClockwise(HexCoord coord) {
        return new HexCoord(-coord.r, coord.q + coord.r);
    }

    public static HexCoord Rotate(HexCoord coord, int clockwiseSteps) {
        var normalized = ((clockwiseSteps % 6) + 6) % 6;
        var value = coord;
        for (var i = 0; i < normalized; i++) {
            value = RotateClockwise(value);
        }

        return value;
    }

    public bool Equals(HexCoord other) {
        return q == other.q && r == other.r;
    }

    public override bool Equals(object obj) {
        return obj is HexCoord other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            return (q * 397) ^ r;
        }
    }
}

[Serializable]
public struct HexSide {
    public int value;

    public HexSide(int value) {
        this.value = ((value % 6) + 6) % 6;
    }
}

[Serializable]
public struct RuneShapeCell {
    public int q;
    public int r;

    public RuneShapeCell(int q, int r) {
        this.q = q;
        this.r = r;
    }

    public HexCoord ToCoord() {
        return new HexCoord(q, r);
    }
}

[Serializable]
public class RunePlacedGlyph {
    public string placementId;
    public string glyphId;
    public HexCoord anchor;
    public int rotation;
}

[Serializable]
public class RuneLink {
    public string fromPlacementId;
    public string toPlacementId;
    public int fromSide;
    public int toSide;
}

[Serializable]
public class RuneBoardData {
    public int radius = 5;
    public List<RunePlacedGlyph> glyphs = new List<RunePlacedGlyph>();
    public List<RuneLink> links = new List<RuneLink>();

    public RuneBoardData Clone() {
        var clone = new RuneBoardData {
            radius = radius,
            glyphs = new List<RunePlacedGlyph>(glyphs.Count),
            links = new List<RuneLink>(links.Count)
        };

        for (var i = 0; i < glyphs.Count; i++) {
            var g = glyphs[i];
            clone.glyphs.Add(new RunePlacedGlyph {
                placementId = g.placementId,
                glyphId = g.glyphId,
                anchor = g.anchor,
                rotation = g.rotation,
            });
        }

        for (var i = 0; i < links.Count; i++) {
            var l = links[i];
            clone.links.Add(new RuneLink {
                fromPlacementId = l.fromPlacementId,
                toPlacementId = l.toPlacementId,
                fromSide = l.fromSide,
                toSide = l.toSide,
            });
        }

        return clone;
    }
}

[Serializable]
public class RuneSpellDraft {
    public string name;
    public int version = 1;
    public RuneBoardData board = new RuneBoardData();
    public string updatedAtUtc;
}

