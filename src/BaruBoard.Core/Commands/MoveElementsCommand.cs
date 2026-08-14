using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Commands;

public readonly record struct ElementMove(BoardElement Element, PointD Before, PointD After);

/// <summary>
/// One user operation that repositioned any number of elements: a group drag, an
/// alignment or a distribution.
/// </summary>
public sealed class MoveElementsCommand : IUndoableCommand
{
    private readonly ElementMove[] _moves;

    public MoveElementsCommand(IEnumerable<ElementMove> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);
        _moves = [.. moves];

        if (_moves.Length == 0)
            throw new ArgumentException("A move command needs at least one element.", nameof(moves));
    }

    public void Execute()
    {
        foreach (var move in _moves)
            move.Element.MoveTo(move.After);
    }

    public void Undo()
    {
        foreach (var move in _moves)
            move.Element.MoveTo(move.Before);
    }
}
