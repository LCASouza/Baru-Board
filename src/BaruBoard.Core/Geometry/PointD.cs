namespace BaruBoard.Core.Geometry;

public readonly record struct PointD(double X, double Y)
{
    public static PointD operator +(PointD point, VectorD vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    public static PointD operator -(PointD point, VectorD vector) =>
        new(point.X - vector.X, point.Y - vector.Y);

    public static VectorD operator -(PointD a, PointD b) =>
        new(a.X - b.X, a.Y - b.Y);
}
