using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Tools;

public class MultiSelectionToolTests
{
    private const double Tolerance = 1e-9;

    private sealed record Scene(
        SelectionTool Tool,
        BoardDocument Document,
        SelectionState Selection,
        CommandHistory History,
        EditorInteractionState Interaction,
        GridSettings Grid);

    private static Scene CreateScene(double zoom = 1.0, double gridStep = 20, bool snap = false)
    {
        var document = new BoardDocument();
        var viewport = new Viewport
        {
            Position = new PointD(0, 0),
            Zoom = zoom,
            ViewportSize = new SizeD(1200, 800),
        };

        var selection = new SelectionState();
        var history = new CommandHistory();
        var grid = new GridSettings { LogicalStep = gridStep, SnapEnabled = snap };
        var interaction = new EditorInteractionState();
        var tool = new SelectionTool(
            document, viewport, selection, history, new SnapContext(grid, interaction), interaction);

        return new Scene(tool, document, selection, history, interaction, grid);
    }

    private static RectangleElement AddRectangle(BoardDocument document, double x, double y, double size = 60)
    {
        var element = new RectangleElement(new RectD(x, y, size, size));
        document.AddElement(element);
        return element;
    }

    [Fact]
    public void ModifierClick_AddsToTheSelection()
    {
        var scene = CreateScene();
        var first = AddRectangle(scene.Document, 0, 0);
        var second = AddRectangle(scene.Document, 200, 0);

        scene.Tool.PointerPressed(new PointD(30, 30));
        scene.Tool.PointerReleased(new PointD(30, 30));

        scene.Interaction.IsMultiSelectModifierDown = true;
        scene.Tool.PointerPressed(new PointD(230, 30));
        scene.Tool.PointerReleased(new PointD(230, 30));

        Assert.Equal([first, second], scene.Selection.Elements);
    }

    [Fact]
    public void ModifierClick_OnSelectedElement_RemovesIt()
    {
        var scene = CreateScene();
        var first = AddRectangle(scene.Document, 0, 0);
        var second = AddRectangle(scene.Document, 200, 0);
        scene.Selection.SelectMany([first, second]);

        scene.Interaction.IsMultiSelectModifierDown = true;
        scene.Tool.PointerPressed(new PointD(30, 30));
        scene.Tool.PointerReleased(new PointD(30, 30));

        Assert.Equal([second], scene.Selection.Elements);
    }

    [Fact]
    public void Marquee_SelectsEveryIntersectedElement()
    {
        var scene = CreateScene();
        var inside = AddRectangle(scene.Document, 100, 100);
        var partial = AddRectangle(scene.Document, 240, 100);
        var outside = AddRectangle(scene.Document, 600, 600);

        scene.Tool.PointerPressed(new PointD(50, 50));
        scene.Tool.PointerMoved(new PointD(260, 260));
        scene.Tool.PointerReleased(new PointD(260, 260));

        Assert.Contains(inside, scene.Selection.Elements);
        Assert.Contains(partial, scene.Selection.Elements);
        Assert.DoesNotContain(outside, scene.Selection.Elements);
        Assert.Null(scene.Selection.MarqueeBounds);
    }

    [Fact]
    public void Marquee_ExposesItsBoundsWhileDragging()
    {
        var scene = CreateScene();
        AddRectangle(scene.Document, 100, 100);

        scene.Tool.PointerPressed(new PointD(300, 300));
        scene.Tool.PointerMoved(new PointD(100, 100));

        var marquee = Assert.NotNull(scene.Selection.MarqueeBounds);
        Assert.Equal(100, marquee.Left, Tolerance);
        Assert.Equal(200, marquee.Width, Tolerance);
    }

    [Fact]
    public void Marquee_WorksInNegativeSpaceAndAtOtherZoom()
    {
        var scene = CreateScene(zoom: 0.5);
        var element = AddRectangle(scene.Document, -400, -400);

        scene.Tool.PointerPressed(new PointD(-250, -250));
        scene.Tool.PointerMoved(new PointD(-150, -150));
        scene.Tool.PointerReleased(new PointD(-150, -150));

        Assert.Equal([element], scene.Selection.Elements);
    }

    [Fact]
    public void GroupDrag_MovesEveryElementByTheSameDelta()
    {
        var scene = CreateScene();
        var first = AddRectangle(scene.Document, 0, 0);
        var second = AddRectangle(scene.Document, 200, 100);
        var line = new LineElement(new PointD(400, 0), new PointD(500, 100));
        scene.Document.AddElement(line);
        scene.Selection.SelectMany([first, second, line]);

        scene.Tool.PointerPressed(new PointD(30, 30));
        scene.Tool.PointerMoved(new PointD(130, 80));
        scene.Tool.PointerReleased(new PointD(130, 80));

        Assert.Equal(100, first.Bounds.X, Tolerance);
        Assert.Equal(300, second.Bounds.X, Tolerance);
        Assert.Equal(new PointD(500, 50), line.Start);
        Assert.Equal(1, scene.History.Count);
    }

    [Fact]
    public void GroupDrag_IsUndoneAsASingleOperation()
    {
        var scene = CreateScene();
        var first = AddRectangle(scene.Document, 0, 0);
        var second = AddRectangle(scene.Document, 200, 100);
        scene.Selection.SelectMany([first, second]);

        scene.Tool.PointerPressed(new PointD(30, 30));
        scene.Tool.PointerMoved(new PointD(330, 230));
        scene.Tool.PointerReleased(new PointD(330, 230));

        scene.History.Undo();

        Assert.Equal(0, first.Bounds.X, Tolerance);
        Assert.Equal(200, second.Bounds.X, Tolerance);
        Assert.Equal(100, second.Bounds.Y, Tolerance);
    }

    [Fact]
    public void SnappedGroupDrag_KeepsRelativeDistancesExactly()
    {
        var scene = CreateScene(snap: true);
        var first = AddRectangle(scene.Document, 3, 7);
        var second = AddRectangle(scene.Document, 137, 61);
        scene.Selection.SelectMany([first, second]);

        var offsetBefore = second.Bounds.Position - first.Bounds.Position;

        scene.Tool.PointerPressed(new PointD(10, 10));
        scene.Tool.PointerMoved(new PointD(117, 94));
        scene.Tool.PointerReleased(new PointD(117, 94));

        var offsetAfter = second.Bounds.Position - first.Bounds.Position;
        Assert.Equal(offsetBefore.X, offsetAfter.X, Tolerance);
        Assert.Equal(offsetBefore.Y, offsetAfter.Y, Tolerance);

        // The anchor of the group, not each element, lands on the grid.
        Assert.Equal(0, first.Bounds.X % scene.Grid.LogicalStep, Tolerance);
    }

    [Fact]
    public void SuppressedSnap_MovesFreely()
    {
        var scene = CreateScene(snap: true);
        var element = AddRectangle(scene.Document, 0, 0);
        scene.Selection.Select(element);
        scene.Interaction.IsSnapSuppressed = true;

        scene.Tool.PointerPressed(new PointD(10, 10));
        scene.Tool.PointerMoved(new PointD(23, 17));
        scene.Tool.PointerReleased(new PointD(23, 17));

        Assert.Equal(13, element.Bounds.X, Tolerance);
        Assert.Equal(7, element.Bounds.Y, Tolerance);
    }

    [Fact]
    public void DeleteSelection_RemovesEverySelectedElementAtOnce()
    {
        var scene = CreateScene();
        var first = AddRectangle(scene.Document, 0, 0);
        var middle = AddRectangle(scene.Document, 100, 0);
        var last = AddRectangle(scene.Document, 200, 0);
        scene.Selection.SelectMany([first, last]);

        Assert.True(scene.Tool.DeleteSelection());

        Assert.Equal([middle], scene.Document.Elements);
        Assert.True(scene.Selection.IsEmpty);
        Assert.Equal(1, scene.History.Count);

        scene.History.Undo();
        Assert.Equal([first, middle, last], scene.Document.Elements);
    }

    [Fact]
    public void HandlesAreIgnoredWhileMoreThanOneElementIsSelected()
    {
        var scene = CreateScene();
        var first = AddRectangle(scene.Document, 0, 0);
        var second = AddRectangle(scene.Document, 200, 0);
        scene.Selection.SelectMany([first, second]);

        // Corner of the first element: would be a resize handle in single selection,
        // but with several elements selected it is just a group drag.
        scene.Tool.PointerPressed(new PointD(60, 60));
        scene.Tool.PointerMoved(new PointD(160, 160));
        scene.Tool.PointerReleased(new PointD(160, 160));

        Assert.Equal(60, first.Bounds.Width, Tolerance);
        Assert.Equal(60, second.Bounds.Width, Tolerance);
        Assert.Equal(100, first.Bounds.X, Tolerance);
        Assert.Equal(300, second.Bounds.X, Tolerance);
    }
}
