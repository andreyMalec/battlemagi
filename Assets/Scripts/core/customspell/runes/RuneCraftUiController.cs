using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneCraftUiController : MonoBehaviour {
    [SerializeField] private RuneCraftEditor editor;
    [SerializeField] private RuneCraftBoardView boardView;
    [SerializeField] private RuneGlyphPaletteView paletteView;
    [SerializeField] private RuneGlyphShapeEditorView glyphShapeEditor;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Toggle linkModeToggle;
    [SerializeField] private TMP_Dropdown fromSideDropdown;
    [SerializeField] private TMP_Dropdown toSideDropdown;
    [SerializeField] private Button saveGlyph;

    private readonly Dictionary<string, RuneGlyphDefinition> _glyphById = new Dictionary<string, RuneGlyphDefinition>();
    private readonly Dictionary<HexCoord, string> _occupiedCells = new Dictionary<HexCoord, string>();

    private string _activePaletteGlyphId = string.Empty;
    private bool _dragActive;
    private bool _suppressNextClick;
    private HexCoord _dragCurrentCell;

    private void Start() {
        BuildGlyphCache();
        paletteView.Bind(editor.GlyphCatalog);
        paletteView.GlyphClicked += HandlePaletteGlyphClicked;
        saveGlyph.onClick.AddListener(SaveGlyphShape);

        boardView?.BuildGrid(editor.Draft.board.radius);
        if (boardView != null) {
            boardView.CellClicked += HandleBoardCellClick;
            boardView.CellDragStarted += HandleBoardDragStart;
            boardView.CellDragged += HandleBoardDrag;
            boardView.CellDragEnded += HandleBoardDragEnd;
        }

        if (editor.GlyphCatalog.Count > 0) {
            SelectPaletteGlyph(editor.GlyphCatalog[0]);
        }

        RefreshBoard();
        SetStatus("Ready");
    }

    public void Undo() {
        if (editor.Undo()) {
            RefreshBoard();
            SetStatus("Undo");
        }
    }

    public void Redo() {
        if (editor.Redo()) {
            RefreshBoard();
            SetStatus("Redo");
        }
    }

    public void DeleteSelection() {
        if (editor.DeleteSelection()) {
            RefreshBoard();
            SetStatus("Deleted");
        }
    }

    public void RotateSelectionClockwise() {
        if (editor.Selection.Count == 0)
            return;

        var pivot = GetSelectionPivot();
        if (editor.TryRotateSelection(1, pivot)) {
            RefreshBoard();
            SetStatus("Rotated");
        }
    }

    public void RotateSelectionCounterClockwise() {
        if (editor.Selection.Count == 0)
            return;

        var pivot = GetSelectionPivot();
        if (editor.TryRotateSelection(-1, pivot)) {
            RefreshBoard();
            SetStatus("Rotated");
        }
    }

    public void MoveSelectionSide(int side) {
        if (editor.Selection.Count == 0)
            return;

        var delta = SideToDelta(side);
        if (editor.TryMoveSelection(delta)) {
            RefreshBoard();
            SetStatus("Moved");
        }
    }

    public void SaveGlyphShape() {
        glyphShapeEditor?.ApplyToGlyph();
        BuildGlyphCache();
        RefreshBoard();
        SetStatus("Glyph saved");
    }

    private void HandlePaletteGlyphClicked(RuneGlyphDefinition glyph) {
        SelectPaletteGlyph(glyph);
        SetStatus("Glyph selected");
    }

    private void SelectPaletteGlyph(RuneGlyphDefinition glyph) {
        _activePaletteGlyphId = glyph.glyphId;
        paletteView.SetSelectedGlyph(_activePaletteGlyphId);
        glyphShapeEditor?.BindGlyph(glyph);
    }

    private void HandleBoardCellClick(HexCoord cell, PointerEventData eventData) {
        if (_suppressNextClick) {
            _suppressNextClick = false;
            return;
        }

        RebuildOccupiedLookup();
        var hasPlacement = _occupiedCells.TryGetValue(cell, out var placementId);

        if (eventData.button == PointerEventData.InputButton.Right) {
            if (!hasPlacement)
                return;

            editor.SetSelection(new[] { placementId });
            var pivot = GetAnchorByPlacement(placementId);
            if (editor.TryRotateSelection(1, pivot)) {
                RefreshBoard();
                SetStatus("Rotated");
            }
            return;
        }

        if (!string.IsNullOrWhiteSpace(_activePaletteGlyphId)) {
            if (editor.TryAddGlyph(_activePaletteGlyphId, cell)) {
                RefreshBoard();
                SetStatus("Placed");
            }
            else {
                SetStatus("Cannot place");
            }
            return;
        }

        if (!hasPlacement) {
            editor.SetSelection(System.Array.Empty<string>());
            RefreshBoard();
            return;
        }

        if (linkModeToggle.isOn && editor.Selection.Count == 1 && editor.Selection[0] != placementId) {
            var fromSide = fromSideDropdown.value;
            var toSide = toSideDropdown.value;
            if (editor.TryLinkSelectedTo(placementId, fromSide, toSide)) {
                RefreshBoard();
                SetStatus("Linked");
            }
            else {
                SetStatus("Cannot link");
            }
            return;
        }

        editor.SetSelection(new[] { placementId });
        RefreshBoard();
        SetStatus("Selected");
    }

    private void HandleBoardDragStart(HexCoord cell, PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (linkModeToggle.isOn)
            return;

        RebuildOccupiedLookup();
        if (!_occupiedCells.TryGetValue(cell, out var placementId))
            return;

        if (editor.Selection.Count != 1 || editor.Selection[0] != placementId) {
            editor.SetSelection(new[] { placementId });
            RefreshBoard();
        }

        _dragActive = true;
        _dragCurrentCell = cell;
    }

    private void HandleBoardDrag(HexCoord cell, PointerEventData eventData) {
        if (!_dragActive)
            return;

        HexCoord nextCell;
        if (!TryGetCellFromPointer(eventData, out nextCell)) {
            nextCell = cell;
        }

        if (nextCell.q == _dragCurrentCell.q && nextCell.r == _dragCurrentCell.r)
            return;

        var stepDelta = new HexCoord(nextCell.q - _dragCurrentCell.q, nextCell.r - _dragCurrentCell.r);
        if (editor.TryMoveSelection(stepDelta)) {
            _dragCurrentCell = nextCell;
            RefreshBoard();
            SetStatus("Moved");
        }
    }

    private void HandleBoardDragEnd(HexCoord cell, PointerEventData eventData) {
        if (!_dragActive)
            return;

        _dragActive = false;
        _suppressNextClick = true;
        RefreshBoard();
    }

    private void RefreshBoard() {
        RebuildOccupiedLookup();
        var selected = new HashSet<string>();
        for (var i = 0; i < editor.Selection.Count; i++) {
            selected.Add(editor.Selection[i]);
        }

        boardView?.Render(_occupiedCells, selected);
    }

    private void RebuildOccupiedLookup() {
        _occupiedCells.Clear();
        var placed = editor.Draft.board.glyphs;
        for (var i = 0; i < placed.Count; i++) {
            var glyph = placed[i];
            if (!_glyphById.TryGetValue(glyph.glyphId, out var definition))
                continue;

            for (var c = 0; c < definition.shape.Count; c++) {
                var local = definition.shape[c].ToCoord();
                var rotated = HexCoord.Rotate(local, glyph.rotation);
                _occupiedCells[glyph.anchor + rotated] = glyph.placementId;
            }
        }
    }

    private void BuildGlyphCache() {
        _glyphById.Clear();
        for (var i = 0; i < editor.GlyphCatalog.Count; i++) {
            var glyph = editor.GlyphCatalog[i];
            if (string.IsNullOrWhiteSpace(glyph.glyphId))
                continue;
            _glyphById[glyph.glyphId] = glyph;
        }
    }

    private HexCoord GetSelectionPivot() {
        var placementId = editor.Selection[0];
        return GetAnchorByPlacement(placementId);
    }

    private HexCoord GetAnchorByPlacement(string placementId) {
        var list = editor.Draft.board.glyphs;
        for (var i = 0; i < list.Count; i++) {
            if (list[i].placementId == placementId)
                return list[i].anchor;
        }

        return new HexCoord(0, 0);
    }

    private static HexCoord SideToDelta(int side) {
        var normalized = ((side % 6) + 6) % 6;
        switch (normalized) {
            case 0: return new HexCoord(1, 0);
            case 1: return new HexCoord(1, -1);
            case 2: return new HexCoord(0, -1);
            case 3: return new HexCoord(-1, 0);
            case 4: return new HexCoord(-1, 1);
            default: return new HexCoord(0, 1);
        }
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

    private void SetStatus(string text) {
        statusText.text = text;
    }
}

