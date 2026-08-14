using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Viewports;

public class PanTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double positionX, double positionY, double zoom) => new()
    {
        Position = new PointD(positionX, positionY),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    [Fact]
    public void Pan_DraggingRight_MovesCameraLeft()
    {
        var viewport = CreateViewport(0, 0, 1.0);

        viewport.Pan(new VectorD(100, 40));

        Assert.Equal(-100, viewport.Position.X, Tolerance);
        Assert.Equal(-40, viewport.Position.Y, Tolerance);
    }

    [Theory]
    [InlineData(1.0, 100, -100)]
    [InlineData(2.0, 100, -50)]
    [InlineData(0.5, 100, -200)]
    public void Pan_ScalesScreenDeltaByZoom(double zoom, double screenDeltaX, double expectedPositionShiftX)
    {
        var viewport = CreateViewport(500, 300, zoom);

        viewport.Pan(new VectorD(screenDeltaX, 0));

        Assert.Equal(500 + expectedPositionShiftX, viewport.Position.X, Tolerance);
        Assert.Equal(300, viewport.Position.Y, Tolerance);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(3.0)]
    [InlineData(0.25)]
    public void Pan_MovesContentExactlyByScreenDelta(double zoom)
    {
        var viewport = CreateViewport(-250, 175, zoom);
        var worldPoint = new PointD(500, -300);
        var delta = new VectorD(120, -80);
        var screenBefore = viewport.WorldToScreen(worldPoint);

        viewport.Pan(delta);

        var screenAfter = viewport.WorldToScreen(worldPoint);
        Assert.Equal(screenBefore.X + delta.X, screenAfter.X, Tolerance);
        Assert.Equal(screenBefore.Y + delta.Y, screenAfter.Y, Tolerance);
    }

    [Fact]
    public void Pan_DoesNotChangeZoom()
    {
        var viewport = CreateViewport(0, 0, 2.0);

        viewport.Pan(new VectorD(50, 50));

        Assert.Equal(2.0, viewport.Zoom, Tolerance);
    }
}
