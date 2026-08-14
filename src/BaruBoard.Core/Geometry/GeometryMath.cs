namespace BaruBoard.Core.Geometry;

public static class GeometryMath
{
    public static double DistanceToSegment(PointD point, PointD a, PointD b)
    {
        var segment = b - a;
        var lengthSquared = segment.LengthSquared;
        if (lengthSquared == 0)
            return (point - a).Length;

        var toPoint = point - a;
        var t = Math.Clamp((toPoint.X * segment.X + toPoint.Y * segment.Y) / lengthSquared, 0.0, 1.0);
        var closest = a + segment * t;
        return (point - closest).Length;
    }
}
