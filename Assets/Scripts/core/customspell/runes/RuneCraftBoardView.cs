using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RuneCraftBoardView : MonoBehaviour {
    [SerializeField] private RectTransform cellsRoot;
    [SerializeField] private RuneHexCellButton cellPrefab;
    [SerializeField] private float cellSize = 28f;
    [SerializeField] private Color emptyColor = new Color(0.12f, 0.12f, 0.12f, 0.75f);
    [SerializeField] private Color filledColor = new Color(0.32f, 0.32f, 0.9f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(0.95f, 0.85f, 0.2f, 0.95f);

    private readonly Dictionary<HexCoord, RuneHexCellButton> _cells = new Dictionary<HexCoord, RuneHexCellButton>();

    public event Action<HexCoord, PointerEventData> CellClicked;

    public void BuildGrid(int radius) {
        ClearGrid();
        for (var q = -radius; q <= radius; q++) {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++) {
                var coord = new HexCoord(q, r);
                var cell = Instantiate(cellPrefab, cellsRoot);
                cell.Setup(coord, HandleCellClick);
                var pos = AxialToUi(coord, cellSize);
                ((RectTransform)cell.transform).anchoredPosition = pos;
                cell.SetColor(emptyColor);
                _cells[coord] = cell;
            }
        }
    }

    public void Render(Dictionary<HexCoord, string> occupiedToPlacement, HashSet<string> selectedPlacements) {
        foreach (var pair in _cells) {
            var color = emptyColor;
            if (occupiedToPlacement.TryGetValue(pair.Key, out var placementId)) {
                color = selectedPlacements.Contains(placementId) ? selectedColor : filledColor;
            }

            pair.Value.SetColor(color);
        }
    }

    private void HandleCellClick(RuneHexCellButton cell, PointerEventData eventData) {
        CellClicked?.Invoke(cell.coord, eventData);
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


