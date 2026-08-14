using BaruBoard.Core.Boards;

namespace BaruBoard.Core.Commands;

public sealed class AddElementCommand : IUndoableCommand
{
    private readonly BoardDocument _document;
    private readonly BoardElement _element;
    private readonly int _index;

    public AddElementCommand(BoardDocument document, BoardElement element, int index)
    {
        _document = document;
        _element = element;
        _index = index;
    }

    public void Execute() => _document.InsertElement(_index, _element);

    public void Undo() => _document.RemoveElement(_element);
}
