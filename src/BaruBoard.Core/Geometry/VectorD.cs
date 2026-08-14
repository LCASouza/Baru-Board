namespace BaruBoard.Core.Geometry;

public readonly record struct VectorD(double X, double Y)
{
    public double LengthSquared => X * X + Y * Y;

    public double Length => Math.Sqrt(LengthSquared);

    public static VectorD operator +(VectorD a, VectorD b) => new(a.X + b.X, a.Y + b.Y);

    public static VectorD operator -(VectorD a, VectorD b) => new(a.X - b.X, a.Y - b.Y);

    public static VectorD operator -(VectorD vector) => new(-vector.X, -vector.Y);

    public static VectorD operator *(VectorD vector, double scalar) =>
        new(vector.X * scalar, vector.Y * scalar);

    public static VectorD operator *(double scalar, VectorD vector) => vector * scalar;

    public static VectorD operator /(VectorD vector, double scalar) =>
        new(vector.X / scalar, vector.Y / scalar);
}
