namespace BaruBoard.Core.Geometry;

public readonly record struct SizeD(double Width, double Height)
{
    public static SizeD operator /(SizeD size, double scalar) =>
        new(size.Width / scalar, size.Height / scalar);

    public static SizeD operator *(SizeD size, double scalar) =>
        new(size.Width * scalar, size.Height * scalar);
}
