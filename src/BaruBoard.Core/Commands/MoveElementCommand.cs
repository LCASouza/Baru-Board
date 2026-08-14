using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Commands;

public sealed class MoveElementCommand : IUndoableCommand
{
    private readonly BoardElement _element;
    private readonly PointD _before;
    private readonly PointD _after;

    public MoveElementCommand(BoardElement element, PointD before, PointD after)
    {
        _element = element;
        _before = before;
        _after = after;
    }

    public void Execute() => _element.MoveTo(_after);

    public void Undo() => _element.MoveTo(_before);
}
