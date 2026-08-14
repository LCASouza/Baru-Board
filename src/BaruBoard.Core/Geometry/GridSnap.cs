namespace BaruBoard.Core.Geometry;

public static class GridSnap
{
    /// <summary>
    /// Rounds to the nearest grid line. Exact midpoints round away from zero, so
    /// the result never depends on banker's rounding.
    /// </summary>
    public static double SnapValue(double worldValue, double step)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        if (!double.IsFinite(worldValue))
            return worldValue;

        return Math.Round(worldValue / step, MidpointRounding.AwayFromZero) * step;
    }

    public static PointD SnapPoint(PointD worldPoint, double step) =>
        new(SnapValue(worldPoint.X, step), SnapValue(worldPoint.Y, step));
}
