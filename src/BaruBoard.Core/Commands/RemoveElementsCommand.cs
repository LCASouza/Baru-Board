using BaruBoard.Core.Boards;

namespace BaruBoard.Core.Commands;

public readonly record struct RemovedElement(BoardElement Element, int Index);

/// <summary>
/// One or more removals that belong to a single user operation, such as a delete
/// or a whole eraser gesture. Indexes are the ones each element had at the moment
/// it was removed, which is why undo replays them backwards.
/// </summary>
public sealed class RemoveElementsCommand : IUndoableCommand
{
    private readonly BoardDocument _document;
    private readonly RemovedElement[] _removals;

    public RemoveElementsCommand(BoardDocument document, IEnumerable<RemovedElement> removals)
    {
        ArgumentNullException.ThrowIfNull(removals);
        _document = document;
        _removals = [.. removals];

        if (_removals.Length == 0)
            throw new ArgumentException("A removal command needs at least one element.", nameof(removals));
    }

    public void Execute()
    {
        foreach (var removal in _removals)
            _document.RemoveElement(removal.Element);
    }

    public void Undo()
    {
        for (var i = _removals.Length - 1; i >= 0; i--)
            _document.InsertElement(_removals[i].Index, _removals[i].Element);
    }
}
