namespace BaruBoard.Core.Geometry;

public static class ArrowGeometry
{
    public const double DefaultHeadAngle = Math.PI / 6;

    public static (PointD Left, PointD Right) GetHeadPoints(
        PointD start, PointD end, double headLength, double headAngle)
    {
        var direction = end - start;
        var length = direction.Length;
        if (length == 0)
            return (end, end);

        var unit = direction / length;
        var left = end - Rotate(unit, headAngle) * headLength;
        var right = end - Rotate(unit, -headAngle) * headLength;
        return (left, right);
    }

    private static VectorD Rotate(VectorD vector, double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new VectorD(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
    }
}
