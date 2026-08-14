namespace BaruBoard.Core.Geometry;

/// <summary>
/// Placement of the visible grid lines. The displayed step is a multiple of the
/// logical step, so zooming changes only how dense the drawing is.
/// </summary>
public static class GridGeometry
{
    public const double MinScreenSpacing = 12.0;

    public const int MaxLinesPerAxis = 1024;

    public const int MajorLineEvery = 5;

    public static double GetDisplayStep(double logicalStep, double zoom)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalStep);
        if (zoom <= 0 || !double.IsFinite(zoom))
            return logicalStep;

        var step = logicalStep;
        // Doubling keeps every displayed line on a logical grid line.
        while (step * zoom < MinScreenSpacing && step < double.MaxValue / 2)
            step *= 2;

        return step;
    }

    public static IReadOnlyList<double> GetLines(double from, double to, double step)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        if (!double.IsFinite(from) || !double.IsFinite(to) || to < from)
            return [];

        var first = Math.Ceiling(from / step) * step;
        var count = (int)Math.Floor((to - first) / step) + 1;
        if (count <= 0)
            return [];

        count = Math.Min(count, MaxLinesPerAxis);

        var lines = new double[count];
        for (var i = 0; i < count; i++)
            lines[i] = first + i * step;

        return lines;
    }

    public static bool IsMajorLine(double value, double step) =>
        Math.Abs(Math.Round(value / (step * MajorLineEvery)) * (step * MajorLineEvery) - value) < step / 2;
}
