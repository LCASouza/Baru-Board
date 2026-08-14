using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Viewports;

public class ZoomTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void Zoom_SettingAboveMaximum_ClampsToMaximum()
    {
        var viewport = new Viewport { Zoom = 100 };

        Assert.Equal(viewport.Options.MaxZoom, viewport.Zoom, Tolerance);
    }

    [Fact]
    public void Zoom_SettingBelowMinimum_ClampsToMinimum()
    {
        var viewport = new Viewport { Zoom = 0.0001 };

        Assert.Equal(viewport.Options.MinZoom, viewport.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomBy_OneStep_MultipliesZoomByStepFactor()
    {
        var viewport = new Viewport { Zoom = 1.0, ViewportSize = new SizeD(1200, 800) };

        viewport.ZoomBy(new PointD(600, 400), 1);

        Assert.Equal(viewport.Options.ZoomStepFactor, viewport.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomBy_NegativeStep_DividesZoomByStepFactor()
    {
        var viewport = new Viewport { Zoom = 2.0, ViewportSize = new SizeD(1200, 800) };

        viewport.ZoomBy(new PointD(600, 400), -1);

        Assert.Equal(2.0 / viewport.Options.ZoomStepFactor, viewport.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomBy_FractionalStep_SupportsSmoothScrollingDeltas()
    {
        var viewport = new Viewport { Zoom = 1.0, ViewportSize = new SizeD(1200, 800) };

        viewport.ZoomBy(new PointD(600, 400), 0.5);

        Assert.Equal(Math.Pow(viewport.Options.ZoomStepFactor, 0.5), viewport.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomAt_AboveMaximum_ClampsToMaximum()
    {
        var viewport = new Viewport { Zoom = 4.0, ViewportSize = new SizeD(1200, 800) };

        viewport.ZoomAt(new PointD(600, 400), 50);

        Assert.Equal(viewport.Options.MaxZoom, viewport.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomAt_BelowMinimum_ClampsToMinimum()
    {
        var viewport = new Viewport { Zoom = 0.5, ViewportSize = new SizeD(1200, 800) };

        viewport.ZoomAt(new PointD(600, 400), 0.001);

        Assert.Equal(viewport.Options.MinZoom, viewport.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomAt_AlreadyAtMaximum_LeavesPositionUntouched()
    {
        var viewport = new Viewport { ViewportSize = new SizeD(1200, 800) };
        viewport.Zoom = viewport.Options.MaxZoom;
        viewport.Position = new PointD(123.456, -789.012);

        viewport.ZoomAt(new PointD(600, 400), viewport.Options.MaxZoom * 2);

        Assert.Equal(new PointD(123.456, -789.012), viewport.Position);
    }

    [Fact]
    public void ZoomAt_AlreadyAtMinimum_LeavesPositionUntouched()
    {
        var viewport = new Viewport { ViewportSize = new SizeD(1200, 800) };
        viewport.Zoom = viewport.Options.MinZoom;
        viewport.Position = new PointD(-42, 77);

        viewport.ZoomAt(new PointD(300, 200), viewport.Options.MinZoom / 2);

        Assert.Equal(new PointD(-42, 77), viewport.Position);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(1, 0.5)]
    public void Constructor_InvalidZoomRange_Throws(double minZoom, double maxZoom)
    {
        var options = new ViewportOptions { MinZoom = minZoom, MaxZoom = maxZoom };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Viewport(options));
    }

    [Fact]
    public void Constructor_StepFactorNotAboveOne_Throws()
    {
        var options = new ViewportOptions { ZoomStepFactor = 1.0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Viewport(options));
    }
}
