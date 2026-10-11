using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private TMP_Text selectedText;
    [SerializeField] private Button saveGlyph;
    [SerializeField] private Button craftSpell;
    [SerializeField] private SpellDefinition craftedSpell;

    private readonly Dictionary<string, RuneGlyphDefinition> _glyphById = new Dictionary<string, RuneGlyphDefinition>();
    private readonly Dictionary<HexCoord, string> _occupiedCells = new Dictionary<HexCoord, string>();
    private readonly Dictionary<HexCoord, int> _outputByCell = new Dictionary<HexCoord, int>();

    private string _activePaletteGlyphId = string.Empty;
    private bool _dragActive;
    private bool _suppressNextClick;
    private HexCoord _dragCurrentCell;
    private bool _hasHeldCell;
    private string _heldPlacementId = string.Empty;
    private HexCoord _heldCell;

    private void Start() {
        BuildGlyphCache();
        paletteView.Bind(editor.GlyphCatalog);
        paletteView.GlyphClicked += HandlePaletteGlyphClicked;
        saveGlyph.onClick.AddListener(SaveGlyphShape);
        craftSpell.onClick.AddListener(() => {
            if (editor.TryBuildSpell(out var spell, out var error)) {
                craftedSpell = spell;
                SetStatus($"Crafted spell");

                var casters = FindObjectsByType<SpellCasterPlayer>();
                foreach (var caster in casters) {
                    if (!caster.enabled) continue;
                    caster.UpdateAvailableSpells(new List<SpellDefinition> { spell });
                }
            } else {
                SetStatus("Failed to craft spell: " + error);
            }
        });

        boardView?.BuildGrid(editor.Draft.board.radius);
        if (boardView != null) {
            boardView.CellClicked += HandleBoardCellClick;
            boardView.CellDragStarted += HandleBoardDragStart;
            boardView.CellDragged += HandleBoardDrag;
            boardView.CellDragEnded += HandleBoardDragEnd;
        }

        RefreshBoard();
        SetStatus("Ready");
    }

    private void OnDisable() {
        SaveGlyphShape();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Delete)) {
            DeleteSelection();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) {
            Undo();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y)) {
            Redo();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z)) {
            Redo();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A)) {
            editor.SetSelection(editor.Draft.board.glyphs.Select(g => g.placementId).ToArray());
            RefreshBoard();
            SetStatus("Selected all");
        }
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
            UpdateSelectedGlyphName(string.Empty);
        }
    }

    public void RotateSelectionClockwise() {
        if (editor.Selection.Count == 0)
            return;

        var pivot = GetRotationPivot();
        if (editor.TryRotateSelection(1, pivot)) {
            RefreshBoard();
            SetStatus("Rotated");
        }
    }

    public void RotateSelectionCounterClockwise() {
        if (editor.Selection.Count == 0)
            return;

        var pivot = GetRotationPivot();
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
        selectedText.text = glyph.displayName;
    }

    private void SelectPaletteGlyph(RuneGlyphDefinition glyph) {
        _activePaletteGlyphId = glyph.glyphId;
        paletteView.SetSelectedGlyph(_activePaletteGlyphId);
        glyphShapeEditor?.BindGlyph(glyph);
    }

    private void UnselectPaletteGlyph() {
        _activePaletteGlyphId = string.Empty;
        paletteView.SetSelectedGlyph(_activePaletteGlyphId);
    }

    private void UpdateSelectedGlyphName(string placementId) {
        selectedText.text = "";
        var placed = editor.Draft.board.glyphs;
        for (var i = 0; i < placed.Count; i++) {
            var glyph = placed[i];
            if (glyph.placementId != placementId)
                continue;
            if (!_glyphById.TryGetValue(glyph.glyphId, out var definition))
                continue;
            selectedText.text = definition.displayName;
        }
    }

    private void HandleBoardCellClick(HexCoord cell, PointerEventData eventData) {
        RebuildOccupiedLookup();
        var hasPlacement = _occupiedCells.TryGetValue(cell, out var placementId);

        if (eventData.button == PointerEventData.InputButton.Right) {
            if (_dragActive && _hasHeldCell && editor.Selection.Count == 1) {
                var dragPivot = GetRotationPivot();
                if (editor.TryRotateSelection(1, dragPivot)) {
                    RefreshBoard();
                    SetStatus("Rotated");
                }

                return;
            }

            if (!hasPlacement)
                return;

            _heldPlacementId = placementId;
            _heldCell = cell;
            _hasHeldCell = true;

            var pivot = GetRotationPivot();
            if (editor.TryRotateSelection(1, pivot)) {
                RefreshBoard();
                SetStatus("Rotated");
            }

            return;
        }

        if (!hasPlacement) {
            if (!string.IsNullOrWhiteSpace(_activePaletteGlyphId)) {
                if (editor.TryAddGlyph(_activePaletteGlyphId, cell)) {
                    RefreshBoard();
                    SetStatus("Placed");
                    UnselectPaletteGlyph();
                } else {
                    SetStatus("Cannot place");
                    editor.SetSelection(System.Array.Empty<string>());
                }

                return;
            }

            _hasHeldCell = false;
            _heldPlacementId = string.Empty;
            editor.SetSelection(System.Array.Empty<string>());
            UpdateSelectedGlyphName(null);
            RefreshBoard();
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift)) {
            editor.SetSelection(editor.Selection.Contains(placementId)
                ? editor.Selection.Where(id => id != placementId).ToArray()
                : editor.Selection.Concat(new[] { placementId }).ToArray());
        } else if (editor.Selection.Count == 0 || !editor.Selection.Contains(placementId)) {
            editor.SetSelection(new[] { placementId });
        }

        RefreshBoard();
        SetStatus("Selected");
        UpdateSelectedGlyphName(placementId);
    }

    private void HandleBoardDragStart(HexCoord cell, PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        RebuildOccupiedLookup();
        if (!_occupiedCells.TryGetValue(cell, out var placementId))
            return;

        if (editor.Selection.Count == 0 || (editor.Selection.Count == 1 && editor.Selection[0] != placementId)) {
            editor.SetSelection(new[] { placementId });
            RefreshBoard();
        }

        _heldPlacementId = placementId;
        _heldCell = cell;
        _hasHeldCell = true;
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
            _heldCell = nextCell;
            RefreshBoard();
            SetStatus("Moved");
        }
    }

    private void HandleBoardDragEnd(HexCoord cell, PointerEventData eventData) {
        if (!_dragActive)
            return;

        _dragActive = false;
        _hasHeldCell = false;
        _heldPlacementId = string.Empty;
        RefreshBoard();
    }

    private void RefreshBoard() {
        RebuildOccupiedLookup();
        var selected = new HashSet<string>();
        for (var i = 0; i < editor.Selection.Count; i++) {
            selected.Add(editor.Selection[i]);
        }

        boardView?.Render(_occupiedCells, selected, _outputByCell, editor.Draft.board.glyphs, _glyphById);
    }

    private void RebuildOccupiedLookup() {
        _occupiedCells.Clear();
        _outputByCell.Clear();
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

            if (definition.shape.Count > 0) {
                var outputLocal = definition.shape[0].ToCoord();
                var outputCoord = glyph.anchor + HexCoord.Rotate(outputLocal, glyph.rotation);
                var outputSide = RuneGlyphRenderUtils.NormalizeSide(definition.outputSide - glyph.rotation);
                _outputByCell[outputCoord] = outputSide;
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

    private HexCoord GetRotationPivot() {
        return _heldCell;
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