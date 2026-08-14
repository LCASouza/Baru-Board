using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Viewports;

public class ViewportFramingTests
{
    private const double Tolerance = 1e-9;

    private static readonly SizeD ViewportSize = new(1200, 800);

    private static Viewport CreateViewport(double zoom = 1.0) => new()
    {
        Position = new PointD(0, 0),
        Zoom = zoom,
        ViewportSize = ViewportSize,
    };

    [Fact]
    public void FitToContent_CentersTheContent()
    {
        var content = new RectD(-500, -250, 1000, 500);

        var result = Assert.NotNull(ViewportFraming.FitToContent(content, ViewportSize, 48, 0.1, 8));

        var viewport = CreateViewport();
        viewport.Zoom = result.Zoom;
        viewport.Position = result.Position;

        var center = viewport.ScreenToWorld(new PointD(ViewportSize.Width / 2, ViewportSize.Height / 2));
        Assert.Equal(0, center.X, 1e-6);
        Assert.Equal(0, center.Y, 1e-6);
    }

    [Fact]
    public void FitToContent_LeavesTheContentInsideTheViewport()
    {
        var content = new RectD(2000, -3000, 900, 1600);

        var result = Assert.NotNull(ViewportFraming.FitToContent(content, ViewportSize, 48, 0.1, 8));

        var viewport = CreateViewport();
        viewport.Zoom = result.Zoom;
        viewport.Position = result.Position;

        var visible = viewport.VisibleWorldBounds;
        Assert.True(visible.Left <= content.Left);
        Assert.True(visible.Right >= content.Right);
        Assert.True(visible.Top <= content.Top);
        Assert.True(visible.Bottom >= content.Bottom);
    }

    [Fact]
    public void FitToContent_RespectsTheZoomLimits()
    {
        var hugeContent = new RectD(0, 0, 1_000_000, 1_000_000);
        var tinyContent = new RectD(0, 0, 1, 1);

        var zoomedOut = Assert.NotNull(ViewportFraming.FitToContent(hugeContent, ViewportSize, 48, 0.1, 8));
        var zoomedIn = Assert.NotNull(ViewportFraming.FitToContent(tinyContent, ViewportSize, 48, 0.1, 8));

        Assert.Equal(0.1, zoomedOut.Zoom, Tolerance);
        Assert.Equal(8, zoomedIn.Zoom, Tolerance);
    }

    [Fact]
    public void FitToContent_HandlesDegenerateBounds()
    {
        var point = new RectD(100, 100, 0, 0);
        var line = new RectD(0, 50, 400, 0);

        Assert.NotNull(ViewportFraming.FitToContent(point, ViewportSize, 48, 0.1, 8));
        Assert.NotNull(ViewportFraming.FitToContent(line, ViewportSize, 48, 0.1, 8));
    }

    [Fact]
    public void FitToContent_WithoutAViewport_ReturnsNull()
    {
        Assert.Null(ViewportFraming.FitToContent(new RectD(0, 0, 10, 10), new SizeD(0, 0), 48, 0.1, 8));
    }

    [Fact]
    public void ZoomToActualSize_KeepsTheCenteredWorldPoint()
    {
        var viewport = CreateViewport(zoom: 3.5);
        viewport.Position = new PointD(1234, -567);
        var center = new PointD(ViewportSize.Width / 2, ViewportSize.Height / 2);
        var worldBefore = viewport.ScreenToWorld(center);

        viewport.ZoomAt(center, 1.0);

        Assert.Equal(1.0, viewport.Zoom, Tolerance);
        var worldAfter = viewport.ScreenToWorld(center);
        Assert.Equal(worldBefore.X, worldAfter.X, 1e-6);
        Assert.Equal(worldBefore.Y, worldAfter.Y, 1e-6);
    }

    [Fact]
    public void ContentBounds_ReflectTheDocument()
    {
        var document = new BoardDocument();
        Assert.Null(document.GetContentBounds());

        document.AddElement(new RectangleElement(new RectD(-100, -50, 200, 100)));
        document.AddElement(new RectangleElement(new RectD(300, 200, 100, 100)));

        var bounds = Assert.NotNull(document.GetContentBounds());
        Assert.Equal(-100, bounds.Left, Tolerance);
        Assert.Equal(-50, bounds.Top, Tolerance);
        Assert.Equal(400, bounds.Right, Tolerance);
        Assert.Equal(300, bounds.Bottom, Tolerance);
    }
}
