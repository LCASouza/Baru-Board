using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Viewports;

public class WorldScreenConversionTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double positionX, double positionY, double zoom) => new()
    {
        Position = new PointD(positionX, positionY),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    [Fact]
    public void WorldToScreen_MapsViewportPositionToScreenOrigin()
    {
        var viewport = CreateViewport(340, -120, 2.5);

        var screen = viewport.WorldToScreen(new PointD(340, -120));

        Assert.Equal(0, screen.X, Tolerance);
        Assert.Equal(0, screen.Y, Tolerance);
    }

    [Theory]
    [InlineData(0, 0, 1.0, 150, 75, 150, 75)]
    [InlineData(100, 50, 1.0, 130, 60, 30, 10)]
    [InlineData(100, 50, 2.0, 130, 60, 60, 20)]
    [InlineData(0, 0, 0.5, 400, 200, 200, 100)]
    [InlineData(-200, -100, 1.0, -150, -60, 50, 40)]
    [InlineData(-200, -100, 2.0, -250, -160, -100, -120)]
    public void WorldToScreen_TransformsKnownPoints(
        double positionX, double positionY, double zoom,
        double worldX, double worldY,
        double expectedScreenX, double expectedScreenY)
    {
        var viewport = CreateViewport(positionX, positionY, zoom);

        var screen = viewport.WorldToScreen(new PointD(worldX, worldY));

        Assert.Equal(expectedScreenX, screen.X, Tolerance);
        Assert.Equal(expectedScreenY, screen.Y, Tolerance);
    }

    [Theory]
    [InlineData(0, 0, 1.0, 150, 75, 150, 75)]
    [InlineData(100, 50, 1.0, 30, 10, 130, 60)]
    [InlineData(100, 50, 2.0, 60, 20, 130, 60)]
    [InlineData(0, 0, 0.5, 200, 100, 400, 200)]
    [InlineData(-200, -100, 1.0, 50, 40, -150, -60)]
    [InlineData(-200, -100, 2.0, -100, -120, -250, -160)]
    public void ScreenToWorld_TransformsKnownPoints(
        double positionX, double positionY, double zoom,
        double screenX, double screenY,
        double expectedWorldX, double expectedWorldY)
    {
        var viewport = CreateViewport(positionX, positionY, zoom);

        var world = viewport.ScreenToWorld(new PointD(screenX, screenY));

        Assert.Equal(expectedWorldX, world.X, Tolerance);
        Assert.Equal(expectedWorldY, world.Y, Tolerance);
    }

    [Theory]
    [InlineData(0, 0, 1.0, 150, 75)]
    [InlineData(100, 50, 2.0, 130, 60)]
    [InlineData(-320, 480, 0.25, -1500, 2750)]
    [InlineData(8450, -2300, 3.0, 8630, -2210)]
    [InlineData(-1, -1, 7.5, 0.123456789, -0.987654321)]
    public void ScreenToWorld_InvertsWorldToScreen(
        double positionX, double positionY, double zoom,
        double worldX, double worldY)
    {
        var viewport = CreateViewport(positionX, positionY, zoom);
        var original = new PointD(worldX, worldY);

        var roundTrip = viewport.ScreenToWorld(viewport.WorldToScreen(original));

        Assert.Equal(original.X, roundTrip.X, Tolerance);
        Assert.Equal(original.Y, roundTrip.Y, Tolerance);
    }

    [Theory]
    [InlineData(0, 0, 1.0, 150, 75)]
    [InlineData(100, 50, 2.0, 60, 20)]
    [InlineData(-320, 480, 0.25, -900, 350)]
    [InlineData(8450, -2300, 3.0, 512.5, -417.25)]
    public void WorldToScreen_InvertsScreenToWorld(
        double positionX, double positionY, double zoom,
        double screenX, double screenY)
    {
        var viewport = CreateViewport(positionX, positionY, zoom);
        var original = new PointD(screenX, screenY);

        var roundTrip = viewport.WorldToScreen(viewport.ScreenToWorld(original));

        Assert.Equal(original.X, roundTrip.X, Tolerance);
        Assert.Equal(original.Y, roundTrip.Y, Tolerance);
    }
}
