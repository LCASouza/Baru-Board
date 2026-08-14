using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Boards;

public class LineElement : BoardElement
{
    private PointD _start;
    private PointD _end;
    private double _strokeThickness = 2.0;

    public LineElement(PointD start, PointD end)
    {
        _start = start;
        _end = end;
        UpdateBounds();
    }

    public PointD Start
    {
        get => _start;
        set
        {
            _start = value;
            UpdateBounds();
        }
    }

    public PointD End
    {
        get => _end;
        set
        {
            _end = value;
            UpdateBounds();
        }
    }

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

    public override BoardElement CreateCopy() => new LineElement(_start, _end)
    {
        Stroke = Stroke,
        StrokeThickness = StrokeThickness,
        ZIndex = ZIndex,
    };

    public override void MoveTo(PointD position)
    {
        var delta = position - Bounds.Position;
        _start += delta;
        _end += delta;
        UpdateBounds();
    }

    public override void ResizeTo(RectD bounds) =>
        throw new InvalidOperationException("Line elements derive their bounds from their endpoints.");

    public override bool Contains(PointD worldPoint, double worldTolerance = 0.0) =>
        GeometryMath.DistanceToSegment(worldPoint, _start, _end) <= _strokeThickness / 2 + worldTolerance;

    protected void UpdateBounds() => Bounds = ComputeBounds();

    protected virtual RectD ComputeBounds()
    {
        var half = _strokeThickness / 2;
        var minX = Math.Min(_start.X, _end.X) - half;
        var minY = Math.Min(_start.Y, _end.Y) - half;
        var maxX = Math.Max(_start.X, _end.X) + half;
        var maxY = Math.Max(_start.Y, _end.Y) + half;
        return new RectD(minX, minY, maxX - minX, maxY - minY);
    }
}
