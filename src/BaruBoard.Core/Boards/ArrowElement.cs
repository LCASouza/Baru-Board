using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class ArrowElement : LineElement
{
    public ArrowElement(PointD start, PointD end)
        : base(start, end)
    {
    }

    public double HeadLength => Math.Max(12.0, StrokeThickness * 4);

    public override BoardElement CreateCopy() => new ArrowElement(Start, End)
    {
        Stroke = Stroke,
        StrokeThickness = StrokeThickness,
        ZIndex = ZIndex,
    };

    public override bool Contains(PointD worldPoint, double worldTolerance = 0.0)
    {
        if (base.Contains(worldPoint, worldTolerance))
            return true;

        var (left, right) = ArrowGeometry.GetHeadPoints(Start, End, HeadLength, ArrowGeometry.DefaultHeadAngle);
        var reach = StrokeThickness / 2 + worldTolerance;
        return GeometryMath.DistanceToSegment(worldPoint, End, left) <= reach ||
               GeometryMath.DistanceToSegment(worldPoint, End, right) <= reach;
    }

    protected override RectD ComputeBounds()
    {
        var (left, right) = ArrowGeometry.GetHeadPoints(Start, End, HeadLength, ArrowGeometry.DefaultHeadAngle);
        var half = StrokeThickness / 2;
        var minX = Math.Min(Math.Min(Start.X, End.X), Math.Min(left.X, right.X)) - half;
        var minY = Math.Min(Math.Min(Start.Y, End.Y), Math.Min(left.Y, right.Y)) - half;
        var maxX = Math.Max(Math.Max(Start.X, End.X), Math.Max(left.X, right.X)) + half;
        var maxY = Math.Max(Math.Max(Start.Y, End.Y), Math.Max(left.Y, right.Y)) + half;
        return new RectD(minX, minY, maxX - minX, maxY - minY);
    }
}
