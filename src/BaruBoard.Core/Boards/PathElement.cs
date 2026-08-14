using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public sealed class PathElement : BoardElement
{
    private readonly List<PointD> _points;
    private double _strokeThickness = 3.0;
    private int _geometryVersion;

    public PathElement(PointD firstPoint)
    {
        _points = [firstPoint];
        UpdateBounds();
    }

    public IReadOnlyList<PointD> Points => _points;

    /// <summary>
    /// Bumped whenever the drawn shape changes. Comparing the point list would
    /// cost as much as rebuilding it, so consumers that cache derived geometry
    /// compare this instead.
    /// </summary>
    public int GeometryVersion => _geometryVersion;

    public ColorRgba Stroke { get; set; } = new(0, 0, 0);

    public double StrokeThickness
    {
        get => _strokeThickness;
        set
        {
            _strokeThickness = value;
            UpdateBounds();
        }
    }

    public override ElementResizeMode ResizeMode => ElementResizeMode.None;

    public override BoardElement CreateCopy()
    {
        var copy = new PathElement(_points[0])
        {
            Stroke = Stroke,
            StrokeThickness = StrokeThickness,
            ZIndex = ZIndex,
        };

        copy.SetPoints(_points);
        return copy;
    }

    public void AppendPoint(PointD point)
    {
        _points.Add(point);
        _geometryVersion++;

        var half = _strokeThickness / 2;
        var left = Math.Min(Bounds.Left, point.X - half);
        var top = Math.Min(Bounds.Top, point.Y - half);
        var right = Math.Max(Bounds.Right, point.X + half);
        var bottom = Math.Max(Bounds.Bottom, point.Y + half);
        Bounds = new RectD(left, top, right - left, bottom - top);
    }

    public void SetPoints(IReadOnlyList<PointD> points)
    {
        if (points.Count == 0)
            throw new ArgumentException("A path needs at least one point.", nameof(points));

        _points.Clear();
        _points.AddRange(points);
        UpdateBounds();
    }

    public override void MoveTo(PointD position)
    {
        var delta = position - Bounds.Position;
        for (var i = 0; i < _points.Count; i++)
            _points[i] += delta;

        _geometryVersion++;
        Bounds = new RectD(position, Bounds.Size);
    }

    public override void ResizeTo(RectD bounds) =>
        throw new InvalidOperationException("Path elements derive their bounds from their points.");

    // Hit testing walks the raw polyline; the renderer draws a smoothed curve,
    // so the two can diverge slightly near sharp turns.
    public override bool Contains(PointD worldPoint, double worldTolerance = 0.0)
    {
        var bounds = Bounds;
        if (worldPoint.X < bounds.Left - worldTolerance || worldPoint.X > bounds.Right + worldTolerance ||
            worldPoint.Y < bounds.Top - worldTolerance || worldPoint.Y > bounds.Bottom + worldTolerance)
        {
            return false;
        }

        var reach = _strokeThickness / 2 + worldTolerance;
        if (_points.Count == 1)
            return (worldPoint - _points[0]).Length <= reach;

        for (var i = 0; i < _points.Count - 1; i++)
        {
            if (GeometryMath.DistanceToSegment(worldPoint, _points[i], _points[i + 1]) <= reach)
                return true;
        }

        return false;
    }

    private void UpdateBounds()
    {
        _geometryVersion++;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var point in _points)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        var half = _strokeThickness / 2;
        Bounds = new RectD(minX - half, minY - half, maxX - minX + _strokeThickness, maxY - minY + _strokeThickness);
    }
}
