using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Tools;

public class SelectionToolTests
{
    private const double Tolerance = 1e-9;

    private sealed record Scene(
        SelectionTool Tool,
        BoardDocument Document,
        SelectionState Selection,
        Viewport Viewport,
        RectangleElement Element,
        CommandHistory History);

    private static Scene CreateScene(double zoom = 1.0, double positionX = 0, double positionY = 0)
    {
        var document = new BoardDocument();
        var element = new RectangleElement(new RectD(100, 100, 200, 150));
        document.AddElement(element);

        var viewport = new Viewport
        {
            Position = new PointD(positionX, positionY),
            Zoom = zoom,
            ViewportSize = new SizeD(1200, 800),
        };

        var selection = new SelectionState();
        var history = new CommandHistory();
        var tool = new SelectionTool(document, viewport, selection, history, TestEditing.NoSnap(), new EditorInteractionState());
        return new Scene(tool, document, selection, viewport, element, history);
    }

    private static void AssertBounds(RectD expected, RectD actual)
    {
        Assert.Equal(expected.X, actual.X, Tolerance);
        Assert.Equal(expected.Y, actual.Y, Tolerance);
        Assert.Equal(expected.Width, actual.Width, Tolerance);
        Assert.Equal(expected.Height, actual.Height, Tolerance);
    }

    [Fact]
    public void PressOnElement_SelectsIt()
    {
        var scene = CreateScene();

        var changed = scene.Tool.PointerPressed(new PointD(150, 120));

        Assert.True(changed);
        Assert.Same(scene.Element, scene.Selection.Primary);
    }

    [Fact]
    public void ClickOnEmptySpace_ClearsSelectionOnRelease()
    {
        var scene = CreateScene();
        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerReleased(new PointD(150, 120));

        // The selection survives the press so a click can still be told apart
        // from the beginning of a marquee.
        scene.Tool.PointerPressed(new PointD(1000, 700));
        Assert.Same(scene.Element, scene.Selection.Primary);

        var changed = scene.Tool.PointerReleased(new PointD(1000, 700));

        Assert.True(changed);
        Assert.Null(scene.Selection.Primary);
    }

    [Fact]
    public void PressOnOverlap_SelectsTopmost()
    {
        var scene = CreateScene();
        var above = new RectangleElement(new RectD(150, 120, 100, 100)) { ZIndex = 3 };
        scene.Document.AddElement(above);

        scene.Tool.PointerPressed(new PointD(160, 130));

        Assert.Same(above, scene.Selection.Primary);
    }

    [Fact]
    public void ClickWithJitterBelowThreshold_DoesNotMoveElement()
    {
        var scene = CreateScene();
        var initial = scene.Element.Bounds;

        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerMoved(new PointD(151, 121));
        scene.Tool.PointerReleased(new PointD(151, 121));

        AssertBounds(initial, scene.Element.Bounds);
        Assert.Same(scene.Element, scene.Selection.Primary);
    }

    [Fact]
    public void Drag_MovesElementByTotalWorldDelta()
    {
        var scene = CreateScene();

        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerMoved(new PointD(250, 180));
        scene.Tool.PointerReleased(new PointD(250, 180));

        AssertBounds(new RectD(200, 160, 200, 150), scene.Element.Bounds);
    }

    [Fact]
    public void Drag_WithNegativeDelta_MovesElementBackwards()
    {
        var scene = CreateScene();

        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerMoved(new PointD(30, 40));
        scene.Tool.PointerReleased(new PointD(30, 40));

        AssertBounds(new RectD(-20, 20, 200, 150), scene.Element.Bounds);
    }

    [Fact]
    public void Drag_AtZoom2_ConvertsScreenDeltaToWorldDelta()
    {
        var scene = CreateScene(zoom: 2.0);

        // Element top-left (100,100) sits at screen (200,200) with zoom 2.
        scene.Tool.PointerPressed(new PointD(300, 240));
        scene.Tool.PointerMoved(new PointD(400, 300));
        scene.Tool.PointerReleased(new PointD(400, 300));

        AssertBounds(new RectD(150, 130, 200, 150), scene.Element.Bounds);
    }

    [Fact]
    public void ManySmallMoves_ProduceSameResultAsSingleMove()
    {
        var scene = CreateScene();
        var start = new PointD(150, 120);
        var end = new PointD(487, 341);

        scene.Tool.PointerPressed(start);
        for (var i = 1; i <= 50; i++)
        {
            var t = i / 50.0;
            scene.Tool.PointerMoved(new PointD(
                start.X + (end.X - start.X) * t,
                start.Y + (end.Y - start.Y) * t));
        }

        scene.Tool.PointerReleased(end);

        AssertBounds(new RectD(100 + 337, 100 + 221, 200, 150), scene.Element.Bounds);
    }

    [Fact]
    public void ResizeViaBottomRightHandle_GrowsElement()
    {
        var scene = CreateScene();
        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerReleased(new PointD(150, 120));

        // BottomRight handle center is at world (300,250) = screen (300,250) at zoom 1.
        scene.Tool.PointerPressed(new PointD(300, 250));
        scene.Tool.PointerMoved(new PointD(350, 280));
        scene.Tool.PointerReleased(new PointD(350, 280));

        AssertBounds(new RectD(100, 100, 250, 180), scene.Element.Bounds);
    }

    [Fact]
    public void ResizeViaLeftHandle_MovesOnlyLeftEdge()
    {
        var scene = CreateScene();
        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerReleased(new PointD(150, 120));

        // Left handle center is at world (100,175).
        scene.Tool.PointerPressed(new PointD(100, 175));
        scene.Tool.PointerMoved(new PointD(60, 300));
        scene.Tool.PointerReleased(new PointD(60, 300));

        AssertBounds(new RectD(60, 100, 240, 150), scene.Element.Bounds);
    }

    [Fact]
    public void Resize_AtZoom05_ConvertsScreenDeltaToWorldDelta()
    {
        var scene = CreateScene(zoom: 0.5);
        scene.Tool.PointerPressed(new PointD(75, 60));
        scene.Tool.PointerReleased(new PointD(75, 60));

        // BottomRight handle center world (300,250) = screen (150,125) at zoom 0.5.
        scene.Tool.PointerPressed(new PointD(150, 125));
        scene.Tool.PointerMoved(new PointD(175, 140));
        scene.Tool.PointerReleased(new PointD(175, 140));

        AssertBounds(new RectD(100, 100, 250, 180), scene.Element.Bounds);
    }

    [Fact]
    public void Resize_ClampsAtMinimumSize()
    {
        var scene = CreateScene();
        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerReleased(new PointD(150, 120));

        // Right handle center is at world (300,175); drag far past the left edge.
        scene.Tool.PointerPressed(new PointD(300, 175));
        scene.Tool.PointerMoved(new PointD(-500, 175));
        scene.Tool.PointerReleased(new PointD(-500, 175));

        Assert.Equal(SelectionGeometry.MinElementSize, scene.Element.Bounds.Width, Tolerance);
        Assert.Equal(100, scene.Element.Bounds.X, Tolerance);
    }

    [Fact]
    public void DeleteSelection_RemovesElementAndClearsSelection()
    {
        var scene = CreateScene();
        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerReleased(new PointD(150, 120));

        var deleted = scene.Tool.DeleteSelection();

        Assert.True(deleted);
        Assert.Null(scene.Selection.Primary);
        Assert.Empty(scene.Document.Elements);
    }

    [Fact]
    public void DeleteSelection_WithoutSelection_ReturnsFalse()
    {
        var scene = CreateScene();

        Assert.False(scene.Tool.DeleteSelection());
        Assert.Single(scene.Document.Elements);
    }

    [Fact]
    public void HoverCursor_ReflectsHandlesBodyAndEmptySpace()
    {
        var scene = CreateScene();
        scene.Tool.PointerPressed(new PointD(150, 120));
        scene.Tool.PointerReleased(new PointD(150, 120));

        scene.Tool.PointerMoved(new PointD(300, 250));
        Assert.Equal(EditorCursor.ResizeNwSe, scene.Tool.Cursor);

        scene.Tool.PointerMoved(new PointD(200, 180));
        Assert.Equal(EditorCursor.Move, scene.Tool.Cursor);

        scene.Tool.PointerMoved(new PointD(900, 700));
        Assert.Equal(EditorCursor.Default, scene.Tool.Cursor);
    }

    [Fact]
    public void HoverCursor_WithoutSelection_StaysDefaultOverElement()
    {
        var scene = CreateScene();

        scene.Tool.PointerMoved(new PointD(150, 120));

        Assert.Equal(EditorCursor.Default, scene.Tool.Cursor);
    }
}
