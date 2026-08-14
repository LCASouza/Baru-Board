using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class LineElementTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void Contains_PointNearSegmentWithinThickness_ReturnsTrue()
    {
        var line = new LineElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 4 };

        Assert.True(line.Contains(new PointD(50, 2)));
        Assert.False(line.Contains(new PointD(50, 3)));
    }

    [Fact]
    public void Contains_WithTolerance_ExpandsReach()
    {
        var line = new LineElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 2 };

        Assert.False(line.Contains(new PointD(50, 8)));
        Assert.True(line.Contains(new PointD(50, 8), worldTolerance: 8));
    }

    [Fact]
    public void Bounds_IncludeStrokeInflation()
    {
        var line = new LineElement(new PointD(10, 20), new PointD(110, 20)) { StrokeThickness = 6 };

        Assert.Equal(7, line.Bounds.X, Tolerance);
        Assert.Equal(17, line.Bounds.Y, Tolerance);
        Assert.Equal(106, line.Bounds.Width, Tolerance);
        Assert.Equal(6, line.Bounds.Height, Tolerance);
    }

    [Fact]
    public void ChangingEndpoint_UpdatesBounds()
    {
        var line = new LineElement(new PointD(0, 0), new PointD(100, 0));

        line.End = new PointD(100, 200);

        Assert.True(line.Bounds.Bottom >= 200);
    }

    [Fact]
    public void MoveTo_TranslatesBothEndpoints()
    {
        var line = new LineElement(new PointD(10, 10), new PointD(110, 60)) { StrokeThickness = 2 };
        var boundsBefore = line.Bounds;

        line.MoveTo(new PointD(boundsBefore.X + 50, boundsBefore.Y - 30));

        Assert.Equal(60, line.Start.X, Tolerance);
        Assert.Equal(-20, line.Start.Y, Tolerance);
        Assert.Equal(160, line.End.X, Tolerance);
        Assert.Equal(30, line.End.Y, Tolerance);
        Assert.Equal(boundsBefore.Width, line.Bounds.Width, Tolerance);
        Assert.Equal(boundsBefore.Height, line.Bounds.Height, Tolerance);
    }

    [Fact]
    public void ResizeTo_Throws()
    {
        var line = new LineElement(new PointD(0, 0), new PointD(100, 0));

        Assert.False(line.CanResize);
        Assert.Throws<InvalidOperationException>(() => line.ResizeTo(new RectD(0, 0, 50, 50)));
    }
}
