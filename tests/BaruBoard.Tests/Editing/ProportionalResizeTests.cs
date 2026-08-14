using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Editing;

public class ProportionalResizeTests
{
    private const double Tolerance = 1e-9;

    private static readonly RectD Initial = new(100, 100, 200, 100);

    private static RectD Resize(ResizeHandle handle, double deltaX, double deltaY) =>
        SelectionGeometry.Resize(
            Initial, handle, new VectorD(deltaX, deltaY), ElementResizeMode.ProportionalCorners);

    private static void AssertAspectPreserved(RectD result)
    {
        Assert.Equal(Initial.Width / Initial.Height, result.Width / result.Height, Tolerance);
    }

    [Fact]
    public void BottomRight_AnchorsTopLeft()
    {
        var result = Resize(ResizeHandle.BottomRight, 100, 0);

        Assert.Equal(100, result.Left, Tolerance);
        Assert.Equal(100, result.Top, Tolerance);
        Assert.Equal(300, result.Width, Tolerance);
        AssertAspectPreserved(result);
    }

    [Fact]
    public void TopLeft_AnchorsBottomRight()
    {
        var result = Resize(ResizeHandle.TopLeft, -100, 0);

        Assert.Equal(300, result.Right, Tolerance);
        Assert.Equal(200, result.Bottom, Tolerance);
        Assert.Equal(300, result.Width, Tolerance);
        AssertAspectPreserved(result);
    }

    [Fact]
    public void TopRight_AnchorsBottomLeft()
    {
        var result = Resize(ResizeHandle.TopRight, 100, 0);

        Assert.Equal(100, result.Left, Tolerance);
        Assert.Equal(200, result.Bottom, Tolerance);
        Assert.Equal(300, result.Width, Tolerance);
        AssertAspectPreserved(result);
    }

    [Fact]
    public void BottomLeft_AnchorsTopRight()
    {
        var result = Resize(ResizeHandle.BottomLeft, -100, 0);

        Assert.Equal(300, result.Right, Tolerance);
        Assert.Equal(100, result.Top, Tolerance);
        Assert.Equal(300, result.Width, Tolerance);
        AssertAspectPreserved(result);
    }

    [Fact]
    public void TheAxisDraggedFurthestDrivesTheScale()
    {
        var result = Resize(ResizeHandle.BottomRight, 0, 100);

        Assert.Equal(200, result.Height, Tolerance);
        Assert.Equal(400, result.Width, Tolerance);
    }

    [Fact]
    public void ShrinkingStopsAtTheMinimumSizeWithoutFlipping()
    {
        var result = Resize(ResizeHandle.BottomRight, -1000, -1000);

        Assert.True(result.Width >= SelectionGeometry.MinElementSize);
        Assert.True(result.Height >= SelectionGeometry.MinElementSize);
        Assert.Equal(100, result.Left, Tolerance);
        AssertAspectPreserved(result);
    }

    [Fact]
    public void SideHandlesAreNotOfferedForProportionalElements()
    {
        var viewport = new Viewport
        {
            Position = new PointD(0, 0),
            Zoom = 1,
            ViewportSize = new SizeD(800, 600),
        };

        var side = SelectionGeometry.GetHandleCenter(Initial, ResizeHandle.Right);
        var corner = SelectionGeometry.GetHandleCenter(Initial, ResizeHandle.BottomRight);

        Assert.Null(SelectionGeometry.HitTestHandles(
            Initial, side, viewport, ElementResizeMode.ProportionalCorners));
        Assert.Equal(ResizeHandle.BottomRight, SelectionGeometry.HitTestHandles(
            Initial, corner, viewport, ElementResizeMode.ProportionalCorners));
        Assert.Equal(ResizeHandle.Right, SelectionGeometry.HitTestHandles(
            Initial, side, viewport, ElementResizeMode.Free));
    }

    [Fact]
    public void NoneModeOffersNoHandles()
    {
        Assert.Empty(SelectionGeometry.GetHandles(ElementResizeMode.None));
    }
}
