using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class PathElementTests
{
    private const double Tolerance = 1e-9;

    private static PathElement CreatePath(params PointD[] points)
    {
        var path = new PathElement(points[0]) { StrokeThickness = 4 };
        for (var i = 1; i < points.Length; i++)
            path.AppendPoint(points[i]);
        return path;
    }

    [Fact]
    public void Bounds_CoverPointsInflatedByHalfStroke()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(100, 50));

        Assert.Equal(-2, path.Bounds.X, Tolerance);
        Assert.Equal(-2, path.Bounds.Y, Tolerance);
        Assert.Equal(104, path.Bounds.Width, Tolerance);
        Assert.Equal(54, path.Bounds.Height, Tolerance);
    }

    [Fact]
    public void AppendPoint_ExpandsBoundsIncrementally()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(100, 50));

        path.AppendPoint(new PointD(200, -30));

        Assert.Equal(-2, path.Bounds.X, Tolerance);
        Assert.Equal(-32, path.Bounds.Y, Tolerance);
        Assert.Equal(202, path.Bounds.Right, Tolerance);
        Assert.Equal(52, path.Bounds.Bottom, Tolerance);
    }

    [Fact]
    public void SetPoints_RecomputesBoundsFromScratch()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(500, 500));

        path.SetPoints([new PointD(10, 10), new PointD(20, 20)]);

        Assert.Equal(8, path.Bounds.X, Tolerance);
        Assert.Equal(22, path.Bounds.Right, Tolerance);
        Assert.Equal(2, path.Points.Count);
    }

    [Fact]
    public void MoveTo_TranslatesAllPointsAndBounds()
    {
        var path = CreatePath(new PointD(10, 10), new PointD(110, 60));
        var sizeBefore = path.Bounds.Size;

        path.MoveTo(new PointD(path.Bounds.X + 100, path.Bounds.Y - 50));

        Assert.Equal(new PointD(110, -40), path.Points[0]);
        Assert.Equal(new PointD(210, 10), path.Points[1]);
        Assert.Equal(sizeBefore, path.Bounds.Size);
    }

    [Fact]
    public void Contains_NearSegment_ReturnsTrue()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(100, 0), new PointD(100, 100));

        Assert.True(path.Contains(new PointD(50, 1)));
        Assert.True(path.Contains(new PointD(101, 50)));
    }

    [Fact]
    public void Contains_FarFromAllSegments_ReturnsFalse()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(100, 0), new PointD(100, 100));

        Assert.False(path.Contains(new PointD(30, 40)));
    }

    [Fact]
    public void Contains_WithTolerance_ExpandsReach()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(100, 0));

        Assert.False(path.Contains(new PointD(50, 10)));
        Assert.True(path.Contains(new PointD(50, 10), worldTolerance: 8));
    }

    [Fact]
    public void Contains_SinglePoint_UsesRadius()
    {
        var path = new PathElement(new PointD(50, 50)) { StrokeThickness = 6 };

        Assert.True(path.Contains(new PointD(52, 50)));
        Assert.False(path.Contains(new PointD(56, 50)));
    }

    [Fact]
    public void ResizeTo_Throws()
    {
        var path = CreatePath(new PointD(0, 0), new PointD(10, 10));

        Assert.False(path.CanResize);
        Assert.Throws<InvalidOperationException>(() => path.ResizeTo(new RectD(0, 0, 50, 50)));
    }
}
