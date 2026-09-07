using System.Collections.Generic;

public class RuneBoardHistory {
    private readonly Stack<RuneBoardData> _undo = new Stack<RuneBoardData>();
    private readonly Stack<RuneBoardData> _redo = new Stack<RuneBoardData>();

    public void PushSnapshot(RuneBoardData board) {
        _undo.Push(board.Clone());
        _redo.Clear();
    }

    public bool TryUndo(RuneBoardData current, out RuneBoardData restored) {
        restored = null;
        if (_undo.Count == 0)
            return false;

        _redo.Push(current.Clone());
        restored = _undo.Pop().Clone();
        return true;
    }

    public bool TryRedo(RuneBoardData current, out RuneBoardData restored) {
        restored = null;
        if (_redo.Count == 0)
            return false;

        _undo.Push(current.Clone());
        restored = _redo.Pop().Clone();
        return true;
    }

    public void Clear() {
        _undo.Clear();
        _redo.Clear();
    }
}

