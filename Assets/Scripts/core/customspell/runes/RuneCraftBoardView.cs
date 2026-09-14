using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RuneCraftBoardView : MonoBehaviour {
    [SerializeField] private RectTransform cellsRoot;
    [SerializeField] private RuneHexCellButton cellPrefab;
    [SerializeField] private float cellSize = 28f;
    [SerializeField] private Color emptyColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);
    [SerializeField] private List<RuneGlyphColor> glyphTypeColors = new List<RuneGlyphColor>();
    [SerializeField] private Color glyphBorderColor = new Color(0.08f, 0.08f, 0.14f, 1f);
    [SerializeField] private Color selectedBorderColor = new Color(1f, 0.95f, 0.55f, 1f);
    [SerializeField] private Color outputMarkerColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Sprite outputIconSprite;
    [SerializeField] private Sprite outputTriangleSprite;

    private readonly Dictionary<HexCoord, RuneHexCellButton> _cells = new Dictionary<HexCoord, RuneHexCellButton>();

    public event Action<HexCoord, PointerEventData> CellClicked;
    public event Action<HexCoord, PointerEventData> CellPointerDown;
    public event Action<HexCoord, PointerEventData> CellDragStarted;
    public event Action<HexCoord, PointerEventData> CellDragged;
    public event Action<HexCoord, PointerEventData> CellDragEnded;

    public void BuildGrid(int radius) {
        ClearGrid();
        for (var q = -radius; q <= radius; q++) {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++) {
                var coord = new HexCoord(q, r);
                var cell = Instantiate(cellPrefab, cellsRoot);
                cell.Setup(coord, HandleCellClick, HandleCellPointerDown, HandleCellDragStart, HandleCellDrag,
                    HandleCellDragEnd);
                var pos = AxialToUi(coord, cellSize);
                ((RectTransform)cell.transform).anchoredPosition = pos;
                cell.SetColor(emptyColor);
                _cells[coord] = cell;
            }
        }
    }

    public void Render(
        Dictionary<HexCoord, string> occupiedToPlacement,
        HashSet<string> selectedPlacements,
        Dictionary<HexCoord, int> outputByCell,
        List<RunePlacedGlyph> placed,
        Dictionary<string, RuneGlyphDefinition> glyphById
    ) {
        foreach (var pair in _cells) {
            var fill = emptyColor;
            var isOutput = false;
            var outputSide = 0;

            if (occupiedToPlacement.TryGetValue(pair.Key, out var placementId)) {
                var selected = selectedPlacements.Contains(placementId);
                var placedGlyph = placed.Find(it => it.placementId == placementId);
                fill = glyphById.TryGetValue(placedGlyph.glyphId, out var glyphDef) ? glyphTypeColors.Find(it => glyphDef.glyphType == it.type).color : Color.black;
                var border = selected ? selectedBorderColor : glyphBorderColor;
                var contourMask = RuneGlyphRenderUtils.BuildContourMask(pair.Key, placementId, occupiedToPlacement);

                if (outputByCell.TryGetValue(pair.Key, out outputSide)) {
                    isOutput = true;
                }
                pair.Value.SetContour(border, contourMask, selected);
            } else {
                pair.Value.SetContour(glyphBorderColor, null, false);
            }

            pair.Value.SetColor(fill);
            pair.Value.SetOutputMarker(isOutput, outputSide, outputMarkerColor, outputIconSprite, outputTriangleSprite);
        }
    }

    private void HandleCellClick(RuneHexCellButton cell, PointerEventData eventData) {
        CellClicked?.Invoke(cell.coord, eventData);
    }

    private void HandleCellPointerDown(RuneHexCellButton cell, PointerEventData eventData) {
        CellPointerDown?.Invoke(cell.coord, eventData);
    }

    private void HandleCellDragStart(RuneHexCellButton cell, PointerEventData eventData) {
        CellDragStarted?.Invoke(cell.coord, eventData);
    }

    private void HandleCellDrag(RuneHexCellButton cell, PointerEventData eventData) {
        CellDragged?.Invoke(cell.coord, eventData);
    }

    private void HandleCellDragEnd(RuneHexCellButton cell, PointerEventData eventData) {
        CellDragEnded?.Invoke(cell.coord, eventData);
    }

    private void ClearGrid() {
        _cells.Clear();
        for (var i = cellsRoot.childCount - 1; i >= 0; i--) {
            Destroy(cellsRoot.GetChild(i).gameObject);
        }
    }

    private static Vector2 AxialToUi(HexCoord coord, float size) {
        var x = size * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) * 0.5f * coord.r);
        var y = size * (1.5f * coord.r);
        return new Vector2(x, y);
    }
}

[Serializable]
public struct RuneGlyphColor {
    public RuneGlyphType type;
    public Color color;
}
