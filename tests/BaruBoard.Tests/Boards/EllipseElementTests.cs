using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class EllipseElementTests
{
    private static EllipseElement CreateEllipse() => new(new RectD(100, 100, 200, 100));

    [Fact]
    public void Contains_Center_ReturnsTrue()
    {
        Assert.True(CreateEllipse().Contains(new PointD(200, 150)));
    }

    [Fact]
    public void Contains_PointInsideEllipse_ReturnsTrue()
    {
        Assert.True(CreateEllipse().Contains(new PointD(250, 170)));
    }

    [Fact]
    public void Contains_BoundsCornerOutsideEllipse_ReturnsFalse()
    {
        // The corner is inside the bounding box but outside the ellipse itself.
        Assert.False(CreateEllipse().Contains(new PointD(105, 105)));
    }

    [Fact]
    public void Contains_PointOnAxisEdge_ReturnsTrue()
    {
        var ellipse = CreateEllipse();

        Assert.True(ellipse.Contains(new PointD(300, 150)));
        Assert.True(ellipse.Contains(new PointD(200, 100)));
    }

    [Fact]
    public void Contains_JustOutsideEdge_ReturnsFalse()
    {
        Assert.False(CreateEllipse().Contains(new PointD(301, 150)));
    }

    [Fact]
    public void Contains_WithTolerance_ExpandsHitArea()
    {
        var ellipse = CreateEllipse();

        Assert.False(ellipse.Contains(new PointD(303, 150)));
        Assert.True(ellipse.Contains(new PointD(303, 150), worldTolerance: 5));
    }

    [Fact]
    public void Contains_NegativeCoordinates()
    {
        var ellipse = new EllipseElement(new RectD(-300, -200, 100, 100));

        Assert.True(ellipse.Contains(new PointD(-250, -150)));
        Assert.False(ellipse.Contains(new PointD(-295, -195)));
    }
}
