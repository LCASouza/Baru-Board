using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Editing;

public class SelectionGeometryTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double zoom, double positionX = 0, double positionY = 0) => new()
    {
        Position = new PointD(positionX, positionY),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    [Theory]
    [InlineData(ResizeHandle.TopLeft, 100, 50)]
    [InlineData(ResizeHandle.Top, 200, 50)]
    [InlineData(ResizeHandle.TopRight, 300, 50)]
    [InlineData(ResizeHandle.Right, 300, 100)]
    [InlineData(ResizeHandle.BottomRight, 300, 150)]
    [InlineData(ResizeHandle.Bottom, 200, 150)]
    [InlineData(ResizeHandle.BottomLeft, 100, 150)]
    [InlineData(ResizeHandle.Left, 100, 100)]
    public void GetHandleCenter_ReturnsExpectedWorldPosition(ResizeHandle handle, double expectedX, double expectedY)
    {
        var bounds = new RectD(100, 50, 200, 100);

        var center = SelectionGeometry.GetHandleCenter(bounds, handle);

        Assert.Equal(expectedX, center.X, Tolerance);
        Assert.Equal(expectedY, center.Y, Tolerance);
    }

    [Fact]
    public void HitTestHandles_PointOnHandleCenter_ReturnsHandle()
    {
        var bounds = new RectD(100, 50, 200, 100);
        var viewport = CreateViewport(1.0);

        var handle = SelectionGeometry.HitTestHandles(bounds, new PointD(300, 150), viewport);

        Assert.Equal(ResizeHandle.BottomRight, handle);
    }

    [Fact]
    public void HitTestHandles_PointWithinTolerance_ReturnsHandle()
    {
        var bounds = new RectD(100, 50, 200, 100);
        var viewport = CreateViewport(1.0);

        var handle = SelectionGeometry.HitTestHandles(bounds, new PointD(105, 45), viewport);

        Assert.Equal(ResizeHandle.TopLeft, handle);
    }

    [Fact]
    public void HitTestHandles_PointOutsideTolerance_ReturnsNull()
    {
        var bounds = new RectD(100, 50, 200, 100);
        var viewport = CreateViewport(1.0);

        var handle = SelectionGeometry.HitTestHandles(bounds, new PointD(110, 60), viewport);

        Assert.Null(handle);
    }

    [Fact]
    public void HitTestHandles_ToleranceStaysInScreenSpace_AtHighZoom()
    {
        var bounds = new RectD(100, 50, 200, 100);
        var viewport = CreateViewport(4.0);

        // TopLeft handle sits at screen (400, 200); 4 DIPs away is only 1 world unit.
        var handle = SelectionGeometry.HitTestHandles(bounds, new PointD(404, 204), viewport);

        Assert.Equal(ResizeHandle.TopLeft, handle);
    }

    [Fact]
    public void HitTestHandles_ToleranceStaysInScreenSpace_AtLowZoom()
    {
        var bounds = new RectD(100, 50, 200, 100);
        var viewport = CreateViewport(0.5);

        // TopLeft handle sits at screen (50, 25); 4 DIPs away is 8 world units.
        var hit = SelectionGeometry.HitTestHandles(bounds, new PointD(54, 29), viewport);
        var miss = SelectionGeometry.HitTestHandles(bounds, new PointD(58, 33), viewport);

        Assert.Equal(ResizeHandle.TopLeft, hit);
        Assert.Null(miss);
    }

    [Theory]
    [InlineData(ResizeHandle.TopLeft, 110, 120, 190, 80)]
    [InlineData(ResizeHandle.Top, 100, 120, 200, 80)]
    [InlineData(ResizeHandle.TopRight, 100, 120, 210, 80)]
    [InlineData(ResizeHandle.Right, 100, 100, 210, 100)]
    [InlineData(ResizeHandle.BottomRight, 100, 100, 210, 120)]
    [InlineData(ResizeHandle.Bottom, 100, 100, 200, 120)]
    [InlineData(ResizeHandle.BottomLeft, 110, 100, 190, 120)]
    [InlineData(ResizeHandle.Left, 110, 100, 190, 100)]
    public void Resize_EachHandleMovesOnlyItsEdges(
        ResizeHandle handle,
        double expectedX, double expectedY, double expectedWidth, double expectedHeight)
    {
        var initial = new RectD(100, 100, 200, 100);
        var delta = new VectorD(10, 20);

        var result = SelectionGeometry.Resize(initial, handle, delta);

        Assert.Equal(expectedX, result.X, Tolerance);
        Assert.Equal(expectedY, result.Y, Tolerance);
        Assert.Equal(expectedWidth, result.Width, Tolerance);
        Assert.Equal(expectedHeight, result.Height, Tolerance);
    }

    [Fact]
    public void Resize_ShrinkingBelowMinimum_ClampsAtMinimumSize()
    {
        var initial = new RectD(0, 0, 100, 100);

        var result = SelectionGeometry.Resize(initial, ResizeHandle.Right, new VectorD(-200, 0));

        Assert.Equal(0, result.X, Tolerance);
        Assert.Equal(SelectionGeometry.MinElementSize, result.Width, Tolerance);
        Assert.Equal(100, result.Height, Tolerance);
    }

    [Fact]
    public void Resize_DraggingEdgeAcrossOppositeSide_ClampsWithoutFlipping()
    {
        var initial = new RectD(0, 0, 100, 100);

        var result = SelectionGeometry.Resize(initial, ResizeHandle.Left, new VectorD(500, 0));

        Assert.Equal(100 - SelectionGeometry.MinElementSize, result.X, Tolerance);
        Assert.Equal(SelectionGeometry.MinElementSize, result.Width, Tolerance);
        Assert.True(result.Width > 0);
    }

    [Fact]
    public void Resize_CornerShrinkingBothAxes_ClampsBothIndependently()
    {
        var initial = new RectD(0, 0, 100, 50);

        var result = SelectionGeometry.Resize(initial, ResizeHandle.BottomRight, new VectorD(-500, -500));

        Assert.Equal(SelectionGeometry.MinElementSize, result.Width, Tolerance);
        Assert.Equal(SelectionGeometry.MinElementSize, result.Height, Tolerance);
    }

    [Fact]
    public void Resize_WorksInNegativeCoordinateSpace()
    {
        var initial = new RectD(-300, -200, 100, 50);

        var result = SelectionGeometry.Resize(initial, ResizeHandle.BottomRight, new VectorD(25, 10));

        Assert.Equal(-300, result.X, Tolerance);
        Assert.Equal(-200, result.Y, Tolerance);
        Assert.Equal(125, result.Width, Tolerance);
        Assert.Equal(60, result.Height, Tolerance);
    }

    [Fact]
    public void HitTestHandles_OnSmallElement_CornersWinOverSides()
    {
        // A 10x10 world-unit element at zoom 1 packs all handles within tolerance.
        var bounds = new RectD(0, 0, 10, 10);
        var viewport = CreateViewport(1.0);

        var handle = SelectionGeometry.HitTestHandles(bounds, new PointD(5, 0), viewport);

        Assert.Equal(ResizeHandle.TopLeft, handle);
    }
}
