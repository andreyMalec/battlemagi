using System.Collections.Generic;

public static class RuneGlyphRenderUtils {
    private static readonly HexCoord[] SideDeltas = {
        new HexCoord(1, 0),
        new HexCoord(1, -1),
        new HexCoord(0, -1),
        new HexCoord(-1, 0),
        new HexCoord(-1, 1),
        new HexCoord(0, 1)
    };

    private static readonly float[] SideAnglesDeg = {
        0f,
        -60f,
        -120f,
        180f,
        120f,
        60f
    };

    public static HexCoord GetSideDelta(int side) {
        return SideDeltas[NormalizeSide(side)];
    }

    public static int NormalizeSide(int side) {
        return ((side % 6) + 6) % 6;
    }

    public static float GetSideAngleDeg(int side) {
        return SideAnglesDeg[NormalizeSide(side)];
    }

    public static bool[] BuildContourMask(HexCoord cell, string placementId, Dictionary<HexCoord, string> occupiedToPlacement) {
        var mask = new bool[6];
        for (var side = 0; side < 6; side++) {
            var neighbor = cell + SideDeltas[side];
            if (!occupiedToPlacement.TryGetValue(neighbor, out var neighborPlacement) || neighborPlacement != placementId) {
                mask[side] = true;
            }
        }

        return mask;
    }

    public static bool[] BuildContourMask(HexCoord cell, HashSet<HexCoord> shape) {
        var mask = new bool[6];
        for (var side = 0; side < 6; side++) {
            var neighbor = cell + SideDeltas[side];
            if (!shape.Contains(neighbor))
                mask[side] = true;
        }

        return mask;
    }
}


