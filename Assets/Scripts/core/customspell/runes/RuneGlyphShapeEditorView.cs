using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneGlyphShapeEditorView : MonoBehaviour {
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private RuneHexCellButton cellPrefab;
    [SerializeField] private float cellSize = 22f;
    [SerializeField] private int editRadius = 4;
    [SerializeField] private Sprite defaultCellSprite;
    [SerializeField] private Sprite outputCellSprite;
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
    private HexCoord _outputCell;
    private bool _hasHeldCell;
    private HexCoord _heldCell;
    private bool _dragActive;
    private HexCoord _dragCurrentCell;
    private bool _suppressNextClick;

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

        if (glyph.shape.Count > 0) {
            _outputCell = glyph.shape[0].ToCoord();
        }
        else {
            _outputCell = new HexCoord(0, 0);
            _shape.Add(_outputCell);
        }

        if (!_shape.Contains(_outputCell))
            _shape.Add(_outputCell);

        _outputSide = ((glyph.outputSide % 6) + 6) % 6;
        _hasHeldCell = false;
        _dragActive = false;
        Render();
    }

    public void ApplyToGlyph() {
        if (_glyph == null)
            return;

        _glyph.shape.Clear();
        _glyph.shape.Add(new RuneShapeCell(_outputCell.q, _outputCell.r));
        foreach (var cell in _shape) {
            if (cell.q == _outputCell.q && cell.r == _outputCell.r)
                continue;
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
                cell.Setup(coord, HandleCellClick, HandleCellPointerDown, HandleCellDragStart, HandleCellDrag, HandleCellDragEnd);
                if (defaultCellSprite != null)
                    cell.SetSprite(defaultCellSprite);
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

    private void HandleCellPointerDown(RuneHexCellButton cell, PointerEventData eventData) {
        if (!_shape.Contains(cell.coord))
            return;

        _heldCell = cell.coord;
        _hasHeldCell = true;
    }

    private void HandleCellClick(RuneHexCellButton cell, PointerEventData eventData) {
        if (_suppressNextClick) {
            _suppressNextClick = false;
            return;
        }

        var coord = cell.coord;
        if (eventData.button == PointerEventData.InputButton.Right) {
            if (!_shape.Contains(coord) && !_hasHeldCell)
                return;

            if (!_hasHeldCell || !_shape.Contains(_heldCell)) {
                _heldCell = coord;
                _hasHeldCell = true;
            }

            if (TryRotateHeldCell(1)) {
                Render();
            }
            return;
        }

        if (_shape.Contains(coord)) {
            if (coord.q == _outputCell.q && coord.r == _outputCell.r)
                return;
            _shape.Remove(coord);
        }
        else {
            _shape.Add(coord);
        }

        Render();
    }

    private void HandleCellDragStart(RuneHexCellButton cell, PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!_shape.Contains(cell.coord))
            return;

        _heldCell = cell.coord;
        _hasHeldCell = true;
        _dragActive = true;
        _dragCurrentCell = cell.coord;
    }

    private void HandleCellDrag(RuneHexCellButton cell, PointerEventData eventData) {
        if (!_dragActive)
            return;

        HexCoord nextCell;
        if (!TryGetCellFromPointer(eventData, out nextCell)) {
            nextCell = cell.coord;
        }

        if (nextCell.q == _dragCurrentCell.q && nextCell.r == _dragCurrentCell.r)
            return;

        if (!TryMoveHeldCell(nextCell))
            return;

        _dragCurrentCell = nextCell;
        _heldCell = nextCell;
        Render();
    }

    private void HandleCellDragEnd(RuneHexCellButton cell, PointerEventData eventData) {
        if (!_dragActive)
            return;

        _dragActive = false;
        _suppressNextClick = true;
    }

    private void Render() {
        foreach (var pair in _cells) {
            pair.Value.SetColor(_shape.Contains(pair.Key) ? onCellColor : offCellColor);
            var isOutput = pair.Key.q == _outputCell.q && pair.Key.r == _outputCell.r;
            if (isOutput && outputCellSprite != null) {
                pair.Value.SetSprite(outputCellSprite);
            }
            else if (defaultCellSprite != null) {
                pair.Value.SetSprite(defaultCellSprite);
            }
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

    private bool TryMoveHeldCell(HexCoord targetCell) {
        if (!_hasHeldCell || !_shape.Contains(_heldCell))
            return false;

        if (!IsInsideRadius(targetCell))
            return false;

        if (targetCell.q == _heldCell.q && targetCell.r == _heldCell.r)
            return true;

        if (_shape.Contains(targetCell))
            return false;

        _shape.Remove(_heldCell);
        _shape.Add(targetCell);

        if (_heldCell.q == _outputCell.q && _heldCell.r == _outputCell.r)
            _outputCell = targetCell;

        _heldCell = targetCell;
        return true;
    }

    private bool TryRotateHeldCell(int clockwiseSteps) {
        if (!_hasHeldCell || !_shape.Contains(_heldCell))
            return false;

        if (_heldCell.q == _outputCell.q && _heldCell.r == _outputCell.r) {
            _outputSide = ((_outputSide + clockwiseSteps) % 6 + 6) % 6;
            return true;
        }

        var local = new HexCoord(_heldCell.q - _outputCell.q, _heldCell.r - _outputCell.r);
        var rotatedLocal = HexCoord.Rotate(local, clockwiseSteps);
        var rotatedCell = _outputCell + rotatedLocal;
        if (!IsInsideRadius(rotatedCell))
            return false;

        if (_shape.Contains(rotatedCell))
            return false;

        _shape.Remove(_heldCell);
        _shape.Add(rotatedCell);
        _heldCell = rotatedCell;
        return true;
    }

    private bool IsInsideRadius(HexCoord coord) {
        var s = -coord.q - coord.r;
        return Mathf.Max(Mathf.Abs(coord.q), Mathf.Abs(coord.r), Mathf.Abs(s)) <= editRadius;
    }

    private static bool TryGetCellFromPointer(PointerEventData eventData, out HexCoord coord) {
        var target = eventData.pointerCurrentRaycast.gameObject;
        if (target == null)
            target = eventData.pointerEnter;

        if (target != null) {
            var cell = target.GetComponentInParent<RuneHexCellButton>();
            if (cell != null) {
                coord = cell.coord;
                return true;
            }
        }

        coord = new HexCoord(0, 0);
        return false;
    }
}


