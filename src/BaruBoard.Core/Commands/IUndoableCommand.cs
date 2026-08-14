namespace BaruBoard.Core.Commands;

/// <summary>
/// A single reversible board operation. <see cref="Execute"/> applies the final
/// state and is also what redo calls, so it must be safe to run again after
/// <see cref="Undo"/>.
/// </summary>
public interface IUndoableCommand
{
    void Execute();

    void Undo();
}
