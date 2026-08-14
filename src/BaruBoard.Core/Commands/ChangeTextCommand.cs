using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Commands;

public sealed class ChangeTextCommand : IUndoableCommand
{
    private readonly TextElement _element;
    private readonly string _beforeText;
    private readonly SizeD _beforeSize;
    private readonly string _afterText;
    private readonly SizeD _afterSize;

    public ChangeTextCommand(TextElement element, string beforeText, SizeD beforeSize, string afterText, SizeD afterSize)
    {
        _element = element;
        _beforeText = beforeText;
        _beforeSize = beforeSize;
        _afterText = afterText;
        _afterSize = afterSize;
    }

    public void Execute() => Apply(_afterText, _afterSize);

    public void Undo() => Apply(_beforeText, _beforeSize);

    private void Apply(string text, SizeD size)
    {
        _element.Text = text;
        _element.SetMeasuredSize(size);
    }
}
