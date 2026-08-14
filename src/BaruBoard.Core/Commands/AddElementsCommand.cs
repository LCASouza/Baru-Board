using BaruBoard.Core.Boards;

namespace BaruBoard.Core.Commands;

/// <summary>
/// Several insertions belonging to one user operation, such as dropping multiple
/// images at once.
/// </summary>
public sealed class AddElementsCommand : IUndoableCommand
{
    private readonly BoardDocument _document;
    private readonly AddedElement[] _additions;

    public AddElementsCommand(BoardDocument document, IEnumerable<AddedElement> additions)
    {
        ArgumentNullException.ThrowIfNull(additions);
        _document = document;
        _additions = [.. additions];

        if (_additions.Length == 0)
            throw new ArgumentException("An insertion command needs at least one element.", nameof(additions));
    }

    public void Execute()
    {
        foreach (var addition in _additions)
            _document.InsertElement(addition.Index, addition.Element);
    }

    public void Undo()
    {
        for (var i = _additions.Length - 1; i >= 0; i--)
            _document.RemoveElement(_additions[i].Element);
    }
}

public readonly record struct AddedElement(BoardElement Element, int Index);
