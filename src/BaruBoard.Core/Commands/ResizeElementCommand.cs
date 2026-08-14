using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Commands;

public sealed class ResizeElementCommand : IUndoableCommand
{
    private readonly BoardElement _element;
    private readonly RectD _before;
    private readonly RectD _after;

    public ResizeElementCommand(BoardElement element, RectD before, RectD after)
    {
        _element = element;
        _before = before;
        _after = after;
    }

    public void Execute() => _element.ResizeTo(_after);

    public void Undo() => _element.ResizeTo(_before);
}
