using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Tools;

public class PenToolTests
{
    private const double Tolerance = 1e-9;

    private static (PenTool Tool, BoardDocument Document) CreateScene(double zoom = 1.0)
    {
        var document = new BoardDocument();
        var viewport = new Viewport
        {
            Position = new PointD(0, 0),
            Zoom = zoom,
            ViewportSize = new SizeD(1200, 800),
        };
        return (new PenTool(document, viewport, new CommandHistory()), document);
    }

    [Fact]
    public void Stroke_ConvertsPointsToWorldSpace()
    {
        var (tool, document) = CreateScene(zoom: 2.0);

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(200, 100));
        tool.PointerMoved(new PointD(200, 200));
        tool.PointerReleased(new PointD(200, 200));

        var path = Assert.IsType<PathElement>(Assert.Single(document.Elements));
        Assert.Equal(new PointD(50, 50), path.Points[0]);
        Assert.Equal(new PointD(100, 100), path.Points[^1]);
    }

    [Fact]
    public void CaptureFilter_IgnoresSamplesBelowMinDistance()
    {
        var (tool, document) = CreateScene();

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(100.3, 100));
        tool.PointerMoved(new PointD(100.6, 100));
        var path = Assert.IsType<PathElement>(Assert.Single(document.Elements));
        Assert.Single(path.Points);

        tool.PointerMoved(new PointD(101, 100));
        Assert.Equal(2, path.Points.Count);
    }

    [Fact]
    public void ClickWithoutDrag_LeavesSinglePointPath()
    {
        var (tool, document) = CreateScene();

        tool.PointerPressed(new PointD(300, 200));
        tool.PointerReleased(new PointD(300, 200));

        var path = Assert.IsType<PathElement>(Assert.Single(document.Elements));
        Assert.Single(path.Points);
        Assert.Equal(new PointD(300, 200), path.Points[0]);
    }

    [Fact]
    public void Release_SimplifiesCollinearStroke()
    {
        var (tool, document) = CreateScene();

        tool.PointerPressed(new PointD(0, 0));
        for (var x = 1; x <= 100; x++)
            tool.PointerMoved(new PointD(x, 0));
        var path = Assert.IsType<PathElement>(Assert.Single(document.Elements));
        Assert.True(path.Points.Count > 50);

        tool.PointerReleased(new PointD(100, 0));

        Assert.Equal(2, path.Points.Count);
        Assert.Equal(new PointD(0, 0), path.Points[0]);
        Assert.Equal(new PointD(100, 0), path.Points[^1]);
    }

    [Fact]
    public void SimplificationEpsilon_UsesStrokeZoom()
    {
        // At zoom 4 the same 0.5-world-unit wiggle is 2 DIPs on screen, above the
        // 0.75-DIP tolerance, so it must survive simplification.
        var (tool, document) = CreateScene(zoom: 4.0);

        tool.PointerPressed(new PointD(0, 0));
        tool.PointerMoved(new PointD(200, 2));
        tool.PointerMoved(new PointD(400, 0));
        tool.PointerReleased(new PointD(400, 0));

        var path = Assert.IsType<PathElement>(Assert.Single(document.Elements));
        Assert.Equal(3, path.Points.Count);
    }

    [Fact]
    public void ToolStaysActive_SecondStrokeCreatesSecondPath()
    {
        var (tool, document) = CreateScene();

        tool.PointerPressed(new PointD(0, 0));
        tool.PointerMoved(new PointD(50, 0));
        tool.PointerReleased(new PointD(50, 0));

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(150, 100));
        tool.PointerReleased(new PointD(150, 100));

        Assert.Equal(2, document.Elements.Count);
    }

    [Fact]
    public void StrokeCompleted_IsRaisedWithTheFinishedPath()
    {
        var (tool, document) = CreateScene();
        PathElement? completed = null;
        tool.StrokeCompleted += path => completed = path;

        tool.PointerPressed(new PointD(0, 0));
        tool.PointerMoved(new PointD(80, 40));
        tool.PointerReleased(new PointD(80, 40));

        Assert.Same(document.Elements[0], completed);
    }
}
