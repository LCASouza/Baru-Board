using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public abstract class ShapeElement : BoardElement
{
    protected ShapeElement(RectD bounds) => Bounds = bounds;

    public ColorRgba Fill { get; set; } = new(255, 255, 255);

    public ColorRgba Stroke { get; set; } = new(0, 0, 0);

    public double StrokeThickness { get; set; } = 1.0;
}
