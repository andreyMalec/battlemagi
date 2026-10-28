using System;
using System.Collections.Generic;
using UnityEngine;

public class RuneCraftEditor : MonoBehaviour {
    [SerializeField] private RuneGlyphList glyphCatalog;
    [SerializeField] private SpellDefinition baseSpellTemplate;

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
    private RuneSpellAssembler _spellAssembler;
    private bool _selectionDragMoveActive;
    private bool _selectionDragMoveSnapshotPushed;

    public struct RuneGlyphPlacementInput {
        public string GlyphId;
        public HexCoord Anchor;
        public int Rotation;
    }

    public RuneSpellDraft Draft => draft;
    public IReadOnlyList<RuneGlyphDefinition> GlyphCatalog => glyphCatalog.items;
    public IReadOnlyList<string> Selection => _selection;

    private void Awake() {
        draft.board.radius = glyphCatalog.boardRadius;
        _boardService = new RuneBoardService(glyphCatalog.items);
        _spellAssembler = new RuneSpellAssembler(glyphCatalog.items);
        _boardService.RebuildAutoLinks(draft.board);
        StampTime();

        var lastDraft = PlayerPrefs.GetString("Draft", null);
        if (lastDraft != null) {
            LoadDraftJson(lastDraft);
        }
    }

    private float timer;
    private void FixedUpdate() {
        if (timer < 5f) {
            timer += Time.deltaTime;
            return;
        }
        
        StampTime();
        PlayerPrefs.SetString("Draft", SaveDraftJson(false));
        timer = 0f;
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

    public bool TryPasteGlyphs(IReadOnlyList<RuneGlyphPlacementInput> glyphsToPlace) {
        if (glyphsToPlace == null || glyphsToPlace.Count == 0)
            return false;

        PushSnapshot();
        var placedIds = new List<string>(glyphsToPlace.Count);
        for (var i = 0; i < glyphsToPlace.Count; i++) {
            var input = glyphsToPlace[i];
            if (!_boardService.TryPlaceGlyph(draft.board, input.GlyphId, input.Anchor, input.Rotation, out var placementId))
                return UndoInternal();
            placedIds.Add(placementId);
        }

        _boardService.RebuildAutoLinks(draft.board);
        _selection.Clear();
        for (var i = 0; i < placedIds.Count; i++) {
            _selection.Add(placedIds[i]);
        }
        StampTime();
        return true;
    }

    public bool TryMoveSelection(HexCoord delta) {
        if (_selection.Count == 0)
            return false;

        if (_selectionDragMoveActive) {
            if (!_selectionDragMoveSnapshotPushed) {
                PushSnapshot();
                _selectionDragMoveSnapshotPushed = true;
            }

            if (!_boardService.TryMoveGroup(draft.board, _selection, delta))
                return false;

            _boardService.RebuildAutoLinks(draft.board);
            StampTime();
            return true;
        }

        PushSnapshot();
        if (!_boardService.TryMoveGroup(draft.board, _selection, delta))
            return UndoInternal();

        _boardService.RebuildAutoLinks(draft.board);
        StampTime();
        return true;
    }

    public void BeginSelectionDragMove() {
        _selectionDragMoveActive = true;
        _selectionDragMoveSnapshotPushed = false;
    }

    public void EndSelectionDragMove() {
        _selectionDragMoveActive = false;
        _selectionDragMoveSnapshotPushed = false;
    }

    public bool TryRotateSelection(int clockwiseSteps, HexCoord pivot) {
        if (_selection.Count == 0)
            return false;

        if (_selectionDragMoveActive) {
            if (!_selectionDragMoveSnapshotPushed) {
                PushSnapshot();
                _selectionDragMoveSnapshotPushed = true;
            }

            if (!_boardService.TryRotateGroup(draft.board, _selection, pivot, clockwiseSteps))
                return false;

            _boardService.RebuildAutoLinks(draft.board);
            StampTime();
            return true;
        }

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

        EndSelectionDragMove();

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

        EndSelectionDragMove();

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

    public void LoadDraft(RuneSpellDraft loadedDraft) {
        draft = loadedDraft ?? new RuneSpellDraft();
        if (draft.board == null)
            draft.board = new RuneBoardData();
        draft.board.radius = glyphCatalog.boardRadius;
        _boardService.RebuildAutoLinks(draft.board);
        _undo.Clear();
        _redo.Clear();
        EndSelectionDragMove();
        _selection.Clear();
        StampTime();
    }

    public void LoadDraftJson(string json) {
        var loaded = SpellJsonSerializer.FromJson<RuneSpellDraft>(json);
        LoadDraft(loaded);
    }

    public bool ValidateGraph() {
        return !_boardService.HasCycle(draft.board);
    }

    public bool TryBuildSpell(out SpellDefinition spell, out string error) {
        _boardService.RebuildAutoLinks(draft.board);
        return _spellAssembler.TryAssemble(draft.board, baseSpellTemplate, out spell, out error);
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


