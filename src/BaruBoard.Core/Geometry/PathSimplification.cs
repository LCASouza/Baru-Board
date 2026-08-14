namespace BaruBoard.Core.Geometry;

public static class PathSimplification
{
    // Iterative Ramer–Douglas–Peucker; endpoints are always preserved and long
    // strokes cannot overflow the call stack.
    public static List<PointD> Simplify(IReadOnlyList<PointD> points, double epsilon)
    {
        if (points.Count <= 2 || epsilon <= 0)
            return [.. points];

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[points.Count - 1] = true;

        var pending = new Stack<(int Start, int End)>();
        pending.Push((0, points.Count - 1));

        while (pending.Count > 0)
        {
            var (start, end) = pending.Pop();
            if (end - start < 2)
                continue;

            var maxDistance = 0.0;
            var maxIndex = -1;
            for (var i = start + 1; i < end; i++)
            {
                var distance = GeometryMath.DistanceToSegment(points[i], points[start], points[end]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            if (maxDistance > epsilon)
            {
                keep[maxIndex] = true;
                pending.Push((start, maxIndex));
                pending.Push((maxIndex, end));
            }
        }

        var result = new List<PointD>();
        for (var i = 0; i < points.Count; i++)
        {
            if (keep[i])
                result.Add(points[i]);
        }

        return result;
    }
}
