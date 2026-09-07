using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneGlyphShapeEditorView : MonoBehaviour {
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private RuneHexCellButton cellPrefab;
    [SerializeField] private float cellSize = 22f;
    [SerializeField] private int editRadius = 4;
    [SerializeField] private List<Button> outputSideButtons = new List<Button>();
    [SerializeField] private List<Image> outputSideImages = new List<Image>();
    [SerializeField] private Color inactiveSideColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color activeSideColor = new Color(0.95f, 0.65f, 0.2f, 0.95f);
    [SerializeField] private Color offCellColor = new Color(0.11f, 0.11f, 0.11f, 0.7f);
    [SerializeField] private Color onCellColor = new Color(0.32f, 0.9f, 0.5f, 0.92f);

    private readonly Dictionary<HexCoord, RuneHexCellButton> _cells = new Dictionary<HexCoord, RuneHexCellButton>();
    private readonly HashSet<HexCoord> _shape = new HashSet<HexCoord>();

    private RuneGlyphDefinition _glyph;
    private int _outputSide;

    private void Awake() {
        BuildGrid();
        BindSideButtons();
        Render();
    }

    public void BindGlyph(RuneGlyphDefinition glyph) {
        _glyph = glyph;
        _shape.Clear();
        for (var i = 0; i < glyph.shape.Count; i++) {
            _shape.Add(glyph.shape[i].ToCoord());
        }

        _outputSide = ((glyph.outputSide % 6) + 6) % 6;
        Render();
    }

    public void ApplyToGlyph() {
        if (_glyph == null)
            return;

        _glyph.shape.Clear();
        foreach (var cell in _shape) {
            _glyph.shape.Add(new RuneShapeCell(cell.q, cell.r));
        }

        if (_glyph.shape.Count == 0) {
            _glyph.shape.Add(new RuneShapeCell(0, 0));
        }

        _glyph.outputSide = _outputSide;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_glyph);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    public void SetOutputSide(int side) {
        _outputSide = ((side % 6) + 6) % 6;
        Render();
    }

    private void BuildGrid() {
        _cells.Clear();
        for (var i = gridRoot.childCount - 1; i >= 0; i--) {
            Destroy(gridRoot.GetChild(i).gameObject);
        }

        for (var q = -editRadius; q <= editRadius; q++) {
            var minR = System.Math.Max(-editRadius, -q - editRadius);
            var maxR = System.Math.Min(editRadius, -q + editRadius);
            for (var r = minR; r <= maxR; r++) {
                var coord = new HexCoord(q, r);
                var cell = Instantiate(cellPrefab, gridRoot);
                cell.Setup(coord, HandleCellClick);
                ((RectTransform)cell.transform).anchoredPosition = AxialToUi(coord, cellSize);
                _cells[coord] = cell;
            }
        }
    }

    private void BindSideButtons() {
        for (var i = 0; i < outputSideButtons.Count; i++) {
            var side = i;
            outputSideButtons[i].onClick.RemoveAllListeners();
            outputSideButtons[i].onClick.AddListener(() => SetOutputSide(side));
        }
    }

    private void HandleCellClick(RuneHexCellButton cell, PointerEventData eventData) {
        var coord = cell.coord;
        if (_shape.Contains(coord)) {
            _shape.Remove(coord);
        }
        else {
            _shape.Add(coord);
        }

        Render();
    }

    private void Render() {
        foreach (var pair in _cells) {
            pair.Value.SetColor(_shape.Contains(pair.Key) ? onCellColor : offCellColor);
        }

        for (var i = 0; i < outputSideImages.Count; i++) {
            outputSideImages[i].color = i == _outputSide ? activeSideColor : inactiveSideColor;
        }
    }

    private static Vector2 AxialToUi(HexCoord coord, float size) {
        var x = size * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) * 0.5f * coord.r);
        var y = size * (1.5f * coord.r);
        return new Vector2(x, y);
    }
}


