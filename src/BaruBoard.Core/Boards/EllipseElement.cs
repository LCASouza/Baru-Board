using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class EllipseElement : ShapeElement
{
    public EllipseElement(RectD bounds)
        : base(bounds)
    {
    }

    public override BoardElement CreateCopy() => new EllipseElement(Bounds)
    {
        Fill = Fill,
        Stroke = Stroke,
        StrokeThickness = StrokeThickness,
        ZIndex = ZIndex,
    };

    public override bool Contains(PointD worldPoint, double worldTolerance = 0.0)
    {
        var radiusX = Bounds.Width / 2 + worldTolerance;
        var radiusY = Bounds.Height / 2 + worldTolerance;
        if (radiusX <= 0 || radiusY <= 0)
            return false;

        var dx = (worldPoint.X - (Bounds.X + Bounds.Width / 2)) / radiusX;
        var dy = (worldPoint.Y - (Bounds.Y + Bounds.Height / 2)) / radiusY;
        return dx * dx + dy * dy <= 1;
    }
}
