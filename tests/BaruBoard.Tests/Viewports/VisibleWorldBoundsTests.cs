using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Viewports;

public class VisibleWorldBoundsTests
{
    private const double Tolerance = 1e-9;

    [Theory]
    [InlineData(0, 0, 1.0, 1200, 800, 0, 0, 1200, 800)]
    [InlineData(100, 50, 2.0, 1200, 800, 100, 50, 600, 400)]
    [InlineData(100, 50, 0.5, 1200, 800, 100, 50, 2400, 1600)]
    [InlineData(-500, -300, 1.0, 1920, 1080, -500, -300, 1920, 1080)]
    [InlineData(-500, -300, 4.0, 640, 480, -500, -300, 160, 120)]
    [InlineData(8450, -2300, 0.25, 800, 600, 8450, -2300, 3200, 2400)]
    public void VisibleWorldBounds_MatchesPositionAndScaledSize(
        double positionX, double positionY, double zoom,
        double viewportWidth, double viewportHeight,
        double expectedX, double expectedY, double expectedWidth, double expectedHeight)
    {
        var viewport = new Viewport
        {
            Position = new PointD(positionX, positionY),
            Zoom = zoom,
            ViewportSize = new SizeD(viewportWidth, viewportHeight),
        };

        var bounds = viewport.VisibleWorldBounds;

        Assert.Equal(expectedX, bounds.X, Tolerance);
        Assert.Equal(expectedY, bounds.Y, Tolerance);
        Assert.Equal(expectedWidth, bounds.Width, Tolerance);
        Assert.Equal(expectedHeight, bounds.Height, Tolerance);
    }

    [Fact]
    public void VisibleWorldBounds_CoversExactlyTheScreenCorners()
    {
        var viewport = new Viewport
        {
            Position = new PointD(-320, 475),
            Zoom = 2.5,
            ViewportSize = new SizeD(1366, 768),
        };

        var bounds = viewport.VisibleWorldBounds;
        var topLeft = viewport.ScreenToWorld(new PointD(0, 0));
        var bottomRight = viewport.ScreenToWorld(new PointD(1366, 768));

        Assert.Equal(topLeft.X, bounds.Left, Tolerance);
        Assert.Equal(topLeft.Y, bounds.Top, Tolerance);
        Assert.Equal(bottomRight.X, bounds.Right, Tolerance);
        Assert.Equal(bottomRight.Y, bounds.Bottom, Tolerance);
    }
}
