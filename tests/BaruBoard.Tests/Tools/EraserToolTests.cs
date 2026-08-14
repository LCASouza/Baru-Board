using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Tools;

public class EraserToolTests
{
    private static (EraserTool Tool, BoardDocument Document, SelectionState Selection) CreateScene(double zoom = 1.0)
    {
        var document = new BoardDocument();
        var viewport = new Viewport
        {
            Position = new PointD(0, 0),
            Zoom = zoom,
            ViewportSize = new SizeD(1200, 800),
        };
        var selection = new SelectionState();
        return (new EraserTool(document, viewport, selection, new CommandHistory()), document, selection);
    }

    private static PathElement CreateStroke(params PointD[] points)
    {
        var path = new PathElement(points[0]) { StrokeThickness = 3 };
        for (var i = 1; i < points.Length; i++)
            path.AppendPoint(points[i]);
        return path;
    }

    [Fact]
    public void Press_ErasesPathUnderCursor()
    {
        var (tool, document, _) = CreateScene();
        document.AddElement(CreateStroke(new PointD(0, 100), new PointD(200, 100)));

        var erased = tool.PointerPressed(new PointD(100, 100));

        Assert.True(erased);
        Assert.Empty(document.Elements);
    }

    [Fact]
    public void OtherElementTypes_AreNeverErased()
    {
        var (tool, document, _) = CreateScene();
        document.AddElement(new RectangleElement(new RectD(50, 50, 100, 100)));
        document.AddElement(new LineElement(new PointD(0, 100), new PointD(200, 100)));
        document.AddElement(CreateStroke(new PointD(0, 100), new PointD(200, 100)));

        tool.PointerPressed(new PointD(100, 100));

        Assert.Equal(2, document.Elements.Count);
        Assert.DoesNotContain(document.Elements, e => e is PathElement);
    }

    [Fact]
    public void FastDrag_ErasesPathsBetweenSparseEvents()
    {
        var (tool, document, _) = CreateScene();
        document.AddElement(CreateStroke(new PointD(150, 0), new PointD(150, 200)));

        tool.PointerPressed(new PointD(0, 100));
        tool.PointerMoved(new PointD(300, 100));

        Assert.Empty(document.Elements);
    }

    [Fact]
    public void EraserRadius_ScalesWithZoom()
    {
        // Cursor 20 world units away from the stroke: hit at zoom 0.5 where the
        // 12-DIP radius spans 24 world units, missed at zoom 4 where it spans 3.
        var (lowZoomTool, lowZoomDocument, _) = CreateScene(zoom: 0.5);
        lowZoomDocument.AddElement(CreateStroke(new PointD(0, 120), new PointD(200, 120)));
        lowZoomTool.PointerPressed(new PointD(50, 50));
        Assert.Empty(lowZoomDocument.Elements);

        var (highZoomTool, highZoomDocument, _) = CreateScene(zoom: 4.0);
        highZoomDocument.AddElement(CreateStroke(new PointD(0, 120), new PointD(200, 120)));
        highZoomTool.PointerPressed(new PointD(400, 400));
        Assert.Single(highZoomDocument.Elements);
    }

    [Fact]
    public void GestureWithoutHits_LeavesDocumentUntouched()
    {
        var (tool, document, _) = CreateScene();
        document.AddElement(CreateStroke(new PointD(500, 500), new PointD(600, 600)));

        var pressed = tool.PointerPressed(new PointD(0, 0));
        var moved = tool.PointerMoved(new PointD(50, 0));
        tool.PointerReleased(new PointD(50, 0));

        Assert.False(pressed);
        Assert.False(moved);
        Assert.Single(document.Elements);
    }

    [Fact]
    public void ErasingSelectedPath_ClearsSelection()
    {
        var (tool, document, selection) = CreateScene();
        var stroke = CreateStroke(new PointD(0, 100), new PointD(200, 100));
        document.AddElement(stroke);
        selection.Select(stroke);

        tool.PointerPressed(new PointD(100, 100));

        Assert.Null(selection.Primary);
    }

    [Fact]
    public void SingleGesture_ErasesMultipleStrokes()
    {
        var (tool, document, _) = CreateScene();
        document.AddElement(CreateStroke(new PointD(50, 0), new PointD(50, 200)));
        document.AddElement(CreateStroke(new PointD(150, 0), new PointD(150, 200)));
        document.AddElement(CreateStroke(new PointD(250, 0), new PointD(250, 200)));

        tool.PointerPressed(new PointD(0, 100));
        tool.PointerMoved(new PointD(300, 100));
        tool.PointerReleased(new PointD(300, 100));

        Assert.Empty(document.Elements);
    }
}
