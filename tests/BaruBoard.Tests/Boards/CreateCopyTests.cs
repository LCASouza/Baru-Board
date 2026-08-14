using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class CreateCopyTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void Copy_HasNewIdentity()
    {
        var original = new RectangleElement(new RectD(10, 20, 100, 50));

        var copy = original.CreateCopy();

        Assert.NotEqual(original.Id, copy.Id);
        Assert.NotSame(original, copy);
    }

    [Fact]
    public void RectangleCopy_KeepsAppearanceAndZIndex()
    {
        var original = new RectangleElement(new RectD(10, 20, 100, 50))
        {
            Fill = new ColorRgba(1, 2, 3),
            Stroke = new ColorRgba(4, 5, 6),
            StrokeThickness = 7,
            ZIndex = 9,
        };

        var copy = Assert.IsType<RectangleElement>(original.CreateCopy());

        Assert.Equal(original.Bounds, copy.Bounds);
        Assert.Equal(original.Fill, copy.Fill);
        Assert.Equal(original.Stroke, copy.Stroke);
        Assert.Equal(original.StrokeThickness, copy.StrokeThickness, Tolerance);
        Assert.Equal(9, copy.ZIndex);
    }

    [Fact]
    public void MovingCopy_DoesNotAffectOriginal()
    {
        var original = new RectangleElement(new RectD(10, 20, 100, 50));

        var copy = original.CreateCopy();
        copy.MoveTo(new PointD(500, 500));

        Assert.Equal(10, original.Bounds.X, Tolerance);
        Assert.Equal(20, original.Bounds.Y, Tolerance);
    }

    [Fact]
    public void EllipseCopy_PreservesType()
    {
        var original = new EllipseElement(new RectD(0, 0, 80, 40)) { ZIndex = 2 };

        var copy = Assert.IsType<EllipseElement>(original.CreateCopy());

        Assert.Equal(original.Bounds, copy.Bounds);
        Assert.Equal(2, copy.ZIndex);
    }

    [Fact]
    public void LineCopy_KeepsEndpointsIndependently()
    {
        var original = new LineElement(new PointD(0, 0), new PointD(100, 50)) { StrokeThickness = 4 };

        var copy = Assert.IsType<LineElement>(original.CreateCopy());
        copy.End = new PointD(999, 999);

        Assert.Equal(new PointD(100, 50), original.End);
        Assert.Equal(new PointD(0, 0), copy.Start);
        Assert.Equal(4, copy.StrokeThickness, Tolerance);
    }

    [Fact]
    public void ArrowCopy_StaysAnArrow()
    {
        var original = new ArrowElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 3 };

        var copy = original.CreateCopy();

        Assert.IsType<ArrowElement>(copy);
        Assert.Equal(original.Bounds, copy.Bounds);
    }

    [Fact]
    public void TextCopy_KeepsContentAndMeasuredSize()
    {
        var original = new TextElement(new PointD(5, 5), "hello", 24) { ZIndex = 3 };
        original.SetMeasuredSize(new SizeD(120, 30));

        var copy = Assert.IsType<TextElement>(original.CreateCopy());
        copy.Text = "changed";

        Assert.Equal("hello", original.Text);
        Assert.Equal(24, copy.FontSize, Tolerance);
        Assert.Equal(120, copy.Bounds.Width, Tolerance);
        Assert.Equal(30, copy.Bounds.Height, Tolerance);
        Assert.Equal(3, copy.ZIndex);
    }

    [Fact]
    public void PathCopy_UsesItsOwnPointCollection()
    {
        var original = new PathElement(new PointD(0, 0)) { StrokeThickness = 3 };
        original.AppendPoint(new PointD(50, 0));
        original.AppendPoint(new PointD(50, 50));

        var copy = Assert.IsType<PathElement>(original.CreateCopy());
        copy.AppendPoint(new PointD(999, 999));

        Assert.Equal(3, original.Points.Count);
        Assert.Equal(4, copy.Points.Count);
        Assert.Equal(original.Points[1], copy.Points[1]);
    }

    [Fact]
    public void PathCopy_MoveDoesNotDisturbOriginalPoints()
    {
        var original = new PathElement(new PointD(0, 0));
        original.AppendPoint(new PointD(100, 100));

        var copy = original.CreateCopy();
        copy.MoveTo(new PointD(1000, 1000));

        Assert.Equal(new PointD(0, 0), original.Points[0]);
        Assert.Equal(new PointD(100, 100), original.Points[1]);
    }
}
