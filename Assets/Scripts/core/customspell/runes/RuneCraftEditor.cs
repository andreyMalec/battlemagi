using System;
using System.Collections.Generic;
using UnityEngine;

public class RuneCraftEditor : MonoBehaviour {
    [SerializeField] private int boardRadius = 5;
    [SerializeField] private List<RuneGlyphDefinition> glyphCatalog = new List<RuneGlyphDefinition>();

    [SerializeField] private RuneSpellDraft draft = new RuneSpellDraft {
        name = "New Rune Spell",
        version = 1,
        board = new RuneBoardData(),
        updatedAtUtc = "",
    };

    private readonly List<string> _selection = new List<string>();
    private readonly Stack<RuneBoardData> _undo = new Stack<RuneBoardData>();
    private readonly Stack<RuneBoardData> _redo = new Stack<RuneBoardData>();
    private RuneBoardService _boardService;

    public RuneSpellDraft Draft => draft;
    public IReadOnlyList<RuneGlyphDefinition> GlyphCatalog => glyphCatalog;
    public IReadOnlyList<string> Selection => _selection;

    private void Awake() {
        draft.board.radius = boardRadius;
        _boardService = new RuneBoardService(glyphCatalog);
        _boardService.RebuildAutoLinks(draft.board);
        StampTime();
    }

    public bool TryAddGlyph(string glyphId, HexCoord anchor, int rotation = 0) {
        PushSnapshot();
        if (!_boardService.TryPlaceGlyph(draft.board, glyphId, anchor, rotation, out var placementId))
            return UndoInternal();

        _boardService.RebuildAutoLinks(draft.board);
        _selection.Clear();
        _selection.Add(placementId);
        StampTime();
        return true;
    }

    public bool TryLinkSelectedTo(string targetPlacementId, int selectedSide, int targetSide) {
        if (_selection.Count != 1)
            return false;

        PushSnapshot();
        var ok = _boardService.TryLink(draft.board, _selection[0], targetPlacementId, selectedSide, targetSide);
        if (!ok)
            return UndoInternal();

        StampTime();
        return true;
    }

    public bool TryMoveSelection(HexCoord delta) {
        if (_selection.Count == 0)
            return false;

        PushSnapshot();
        if (!_boardService.TryMoveGroup(draft.board, _selection, delta))
            return UndoInternal();

        _boardService.RebuildAutoLinks(draft.board);
        StampTime();
        return true;
    }

    public bool TryRotateSelection(int clockwiseSteps, HexCoord pivot) {
        if (_selection.Count == 0)
            return false;

        PushSnapshot();
        if (!_boardService.TryRotateGroup(draft.board, _selection, pivot, clockwiseSteps))
            return UndoInternal();

        _boardService.RebuildAutoLinks(draft.board);
        StampTime();
        return true;
    }

    public bool DeleteSelection() {
        if (_selection.Count == 0)
            return false;

        PushSnapshot();
        var removedAny = false;
        for (var i = 0; i < _selection.Count; i++) {
            removedAny |= _boardService.RemoveGlyph(draft.board, _selection[i]);
        }

        if (!removedAny)
            return UndoInternal();

        _boardService.RebuildAutoLinks(draft.board);
        _selection.Clear();
        StampTime();
        return true;
    }

    public void SetSelection(IReadOnlyList<string> placementIds) {
        _selection.Clear();
        for (var i = 0; i < placementIds.Count; i++) {
            _selection.Add(placementIds[i]);
        }
    }

    public bool Undo() {
        if (_undo.Count == 0)
            return false;

        _redo.Push(draft.board.Clone());
        draft.board = _undo.Pop().Clone();
        _boardService.RebuildAutoLinks(draft.board);
        _selection.Clear();
        StampTime();
        return true;
    }

    public bool Redo() {
        if (_redo.Count == 0)
            return false;

        _undo.Push(draft.board.Clone());
        draft.board = _redo.Pop().Clone();
        _boardService.RebuildAutoLinks(draft.board);
        _selection.Clear();
        StampTime();
        return true;
    }

    public string SaveDraftJson(bool pretty = true) {
        StampTime();
        return SpellJsonSerializer.ToJson(draft, pretty);
    }

    public void LoadDraftJson(string json) {
        var loaded = SpellJsonSerializer.FromJson<RuneSpellDraft>(json);
        draft = loaded ?? new RuneSpellDraft();
        if (draft.board == null)
            draft.board = new RuneBoardData();
        draft.board.radius = boardRadius;
        _boardService.RebuildAutoLinks(draft.board);
        _undo.Clear();
        _redo.Clear();
        _selection.Clear();
        StampTime();
    }

    public bool TryUnlink(string fromPlacementId, string toPlacementId) {
        PushSnapshot();
        if (!_boardService.RemoveLink(draft.board, fromPlacementId, toPlacementId))
            return UndoInternal();

        StampTime();
        return true;
    }

    public bool ValidateGraph() {
        return !_boardService.HasCycle(draft.board);
    }

    private bool UndoInternal() {
        if (_undo.Count > 0) {
            _redo.Push(draft.board.Clone());
            draft.board = _undo.Pop().Clone();
        }

        return false;
    }

    private void PushSnapshot() {
        _undo.Push(draft.board.Clone());
        _redo.Clear();
    }

    private void StampTime() {
        draft.updatedAtUtc = DateTime.UtcNow.ToString("O");
    }
}


