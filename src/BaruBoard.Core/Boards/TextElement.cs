using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class TextElement : BoardElement
{
    public TextElement(PointD position, string text, double fontSize)
    {
        Text = text;
        FontSize = fontSize;
        Bounds = new RectD(position, new SizeD(0, 0));
    }

    public string Text { get; set; }

    public double FontSize { get; set; }

    public ColorRgba Foreground { get; set; } = new(0, 0, 0);

    public override ElementResizeMode ResizeMode => ElementResizeMode.None;

    public override BoardElement CreateCopy()
    {
        var copy = new TextElement(Bounds.Position, Text, FontSize)
        {
            Foreground = Foreground,
            ZIndex = ZIndex,
        };

        copy.SetMeasuredSize(Bounds.Size);
        return copy;
    }

    public override void ResizeTo(RectD bounds) =>
        throw new InvalidOperationException("Text elements are sized from their content layout.");

    // The size is derived from text layout measured by the UI layer; only the
    // position is authoritative here.
    public void SetMeasuredSize(SizeD size) => Bounds = new RectD(Bounds.Position, size);
}
