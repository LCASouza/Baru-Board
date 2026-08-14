namespace BaruBoard.Core.Geometry;

public readonly record struct QuadraticSegment(PointD Control, PointD End);

/// <summary>
/// Midpoint quadratic Bézier scheme for rendering freehand strokes: original
/// points become control points and consecutive midpoints become the on-curve
/// endpoints, so the stroke starts and ends exactly on the first and last points.
/// The smoothed curve is visual only — hit testing stays on the raw polyline —
/// and the two geometries can diverge slightly near sharp direction changes.
/// </summary>
public static class PathSmoothing
{
    public static IReadOnlyList<QuadraticSegment> GetSegments(IReadOnlyList<PointD> points)
    {
        if (points.Count < 2)
            return [];

        if (points.Count == 2)
            return [new QuadraticSegment(Midpoint(points[0], points[1]), points[1])];

        var segments = new List<QuadraticSegment>(points.Count - 2);
        for (var i = 1; i < points.Count - 1; i++)
        {
            var end = i == points.Count - 2 ? points[i + 1] : Midpoint(points[i], points[i + 1]);
            segments.Add(new QuadraticSegment(points[i], end));
        }

        return segments;
    }

    private static PointD Midpoint(PointD a, PointD b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
}
