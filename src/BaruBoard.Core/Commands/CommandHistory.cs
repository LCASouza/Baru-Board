namespace BaruBoard.Core.Commands;

public sealed class CommandHistory
{
    public const int DefaultCapacity = 200;

    private readonly record struct HistoryEntry(long StateId, IUndoableCommand Command);

    private readonly LinkedList<HistoryEntry> _undo = new();
    private readonly Stack<HistoryEntry> _redo = new();
    private readonly int _capacity;
    private long _nextStateId = 1;

    public CommandHistory()
        : this(DefaultCapacity)
    {
    }

    public CommandHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    /// <summary>
    /// Raised whenever the history moves, which is the only way board content
    /// changes. Viewport movement deliberately does not raise it.
    /// </summary>
    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public int Count => _undo.Count;

    /// <summary>
    /// Identifies the current position in the history; zero means the state the
    /// document had before any operation.
    /// </summary>
    public long CurrentStateId => _undo.Last is { } last ? last.Value.StateId : 0;

    /// <summary>
    /// Registers an operation the caller has already applied, typically a pointer
    /// interaction that mutated the document live while it was happening.
    /// </summary>
    public void Record(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Push(command);
    }

    /// <summary>
    /// Applies an operation and registers it.
    /// </summary>
    public void Execute(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute();
        Push(command);
    }

    public bool Undo()
    {
        if (_undo.Last is not { } last)
            return false;

        _undo.RemoveLast();
        last.Value.Command.Undo();
        _redo.Push(last.Value);
        Changed?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var entry))
            return false;

        entry.Command.Execute();
        _undo.AddLast(entry);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke();
    }

    private void Push(IUndoableCommand command)
    {
        _redo.Clear();
        _undo.AddLast(new HistoryEntry(_nextStateId++, command));
        if (_undo.Count > _capacity)
            _undo.RemoveFirst();

        Changed?.Invoke();
    }
}
