namespace BaruBoard.Core.Geometry;

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public RectD(PointD position, SizeD size)
        : this(position.X, position.Y, size.Width, size.Height)
    {
    }

    public static RectD FromPoints(PointD a, PointD b) => new(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Abs(a.X - b.X),
        Math.Abs(a.Y - b.Y));

    public double Left => X;

    public double Top => Y;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public PointD Position => new(X, Y);

    public SizeD Size => new(Width, Height);

    public bool Contains(PointD point) =>
        point.X >= Left && point.X <= Right &&
        point.Y >= Top && point.Y <= Bottom;

    // Rectangles that only share an edge or corner still count as intersecting.
    public bool Intersects(RectD other) =>
        Left <= other.Right && Right >= other.Left &&
        Top <= other.Bottom && Bottom >= other.Top;
}
