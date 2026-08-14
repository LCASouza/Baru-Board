using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Viewports;

public class ZoomAtCursorTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double positionX, double positionY, double zoom) => new()
    {
        Position = new PointD(positionX, positionY),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    [Theory]
    [InlineData(0, 0, 1.0, 200, 100, 2.0)]
    [InlineData(0, 0, 1.0, 200, 100, 0.5)]
    [InlineData(100, 50, 2.0, 640, 360, 3.5)]
    [InlineData(-500, -300, 0.5, 25, 775, 1.25)]
    [InlineData(8450, -2300, 4.0, 1199, 1, 0.2)]
    [InlineData(-0.5, 0.5, 1.0, 0, 0, 6.0)]
    public void ZoomAt_KeepsWorldPointUnderCursor(
        double positionX, double positionY, double initialZoom,
        double cursorX, double cursorY, double newZoom)
    {
        var viewport = CreateViewport(positionX, positionY, initialZoom);
        var cursor = new PointD(cursorX, cursorY);
        var worldBefore = viewport.ScreenToWorld(cursor);

        viewport.ZoomAt(cursor, newZoom);

        var worldAfter = viewport.ScreenToWorld(cursor);
        Assert.Equal(worldBefore.X, worldAfter.X, Tolerance);
        Assert.Equal(worldBefore.Y, worldAfter.Y, Tolerance);
    }

    [Fact]
    public void ZoomAt_MatchesWorkedExample()
    {
        var viewport = CreateViewport(0, 0, 1.0);

        viewport.ZoomAt(new PointD(200, 100), 2.0);

        Assert.Equal(new PointD(100, 50), viewport.Position);
        var screen = viewport.WorldToScreen(new PointD(200, 100));
        Assert.Equal(200, screen.X, Tolerance);
        Assert.Equal(100, screen.Y, Tolerance);
    }

    [Fact]
    public void ZoomBy_ConsecutiveSteps_KeepWorldPointUnderCursor()
    {
        var viewport = CreateViewport(-1200, 340, 1.0);
        var cursor = new PointD(451, 333);
        var worldBefore = viewport.ScreenToWorld(cursor);

        for (var i = 0; i < 10; i++)
        {
            viewport.ZoomBy(cursor, 1);
        }

        var worldAfter = viewport.ScreenToWorld(cursor);
        Assert.Equal(worldBefore.X, worldAfter.X, Tolerance);
        Assert.Equal(worldBefore.Y, worldAfter.Y, Tolerance);
    }

    [Fact]
    public void ZoomBy_InAndOutSameSteps_RestoresZoom()
    {
        var viewport = CreateViewport(75, -75, 1.5);
        var cursor = new PointD(800, 200);

        viewport.ZoomBy(cursor, 3);
        viewport.ZoomBy(cursor, -3);

        Assert.Equal(1.5, viewport.Zoom, Tolerance);
    }
}
