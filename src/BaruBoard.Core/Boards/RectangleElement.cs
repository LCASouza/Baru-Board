using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class RectangleElement : ShapeElement
{
    public RectangleElement(RectD bounds)
        : base(bounds)
    {
    }

    public override BoardElement CreateCopy() => new RectangleElement(Bounds)
    {
        Fill = Fill,
        Stroke = Stroke,
        StrokeThickness = StrokeThickness,
        ZIndex = ZIndex,
    };
}
