using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Tools;

public class ToolHistoryTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double zoom = 1.0) => new()
    {
        Position = new PointD(0, 0),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    private static PathElement CreateStroke(params PointD[] points)
    {
        var path = new PathElement(points[0]) { StrokeThickness = 3 };
        for (var i = 1; i < points.Length; i++)
            path.AppendPoint(points[i]);
        return path;
    }

    [Fact]
    public void MoveDrag_ProducesOneUndoableEntry()
    {
        var document = new BoardDocument();
        var element = new RectangleElement(new RectD(100, 100, 100, 100));
        document.AddElement(element);
        var history = new CommandHistory();
        var selection = new SelectionState();
        var tool = new SelectionTool(document, CreateViewport(), selection, history, TestEditing.NoSnap(), new EditorInteractionState());

        tool.PointerPressed(new PointD(150, 150));
        for (var i = 1; i <= 20; i++)
            tool.PointerMoved(new PointD(150 + i * 5, 150 + i * 2));
        tool.PointerReleased(new PointD(250, 190));

        Assert.Equal(1, history.Count);
        Assert.Equal(200, element.Bounds.X, Tolerance);

        history.Undo();

        Assert.Equal(100, element.Bounds.X, Tolerance);
        Assert.Equal(100, element.Bounds.Y, Tolerance);
    }

    [Fact]
    public void ClickWithoutDrag_RecordsNothing()
    {
        var document = new BoardDocument();
        document.AddElement(new RectangleElement(new RectD(100, 100, 100, 100)));
        var history = new CommandHistory();
        var tool = new SelectionTool(document, CreateViewport(), new SelectionState(), history, TestEditing.NoSnap(), new EditorInteractionState());

        tool.PointerPressed(new PointD(150, 150));
        tool.PointerMoved(new PointD(151, 151));
        tool.PointerReleased(new PointD(151, 151));

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void ResizeDrag_IsUndoable()
    {
        var document = new BoardDocument();
        var element = new RectangleElement(new RectD(100, 100, 100, 100));
        document.AddElement(element);
        var history = new CommandHistory();
        var selection = new SelectionState();
        var tool = new SelectionTool(document, CreateViewport(), selection, history, TestEditing.NoSnap(), new EditorInteractionState());

        tool.PointerPressed(new PointD(150, 150));
        tool.PointerReleased(new PointD(150, 150));

        tool.PointerPressed(new PointD(200, 200));
        tool.PointerMoved(new PointD(260, 240));
        tool.PointerReleased(new PointD(260, 240));

        Assert.Equal(1, history.Count);
        Assert.Equal(160, element.Bounds.Width, Tolerance);

        history.Undo();

        Assert.Equal(100, element.Bounds.Width, Tolerance);
        Assert.Equal(100, element.Bounds.Height, Tolerance);
    }

    [Fact]
    public void Delete_RestoresElementAtItsOriginalIndex()
    {
        var document = new BoardDocument();
        var first = new RectangleElement(new RectD(0, 0, 50, 50));
        var target = new RectangleElement(new RectD(100, 100, 100, 100));
        var last = new RectangleElement(new RectD(400, 400, 50, 50));
        document.AddElement(first);
        document.AddElement(target);
        document.AddElement(last);

        var history = new CommandHistory();
        var selection = new SelectionState();
        var tool = new SelectionTool(document, CreateViewport(), selection, history, TestEditing.NoSnap(), new EditorInteractionState());

        tool.PointerPressed(new PointD(150, 150));
        tool.PointerReleased(new PointD(150, 150));
        tool.DeleteSelection();

        Assert.Equal([first, last], document.Elements);

        history.Undo();

        Assert.Equal([first, target, last], document.Elements);
    }

    [Fact]
    public void ShapeCreation_IsUndoableAndRedoable()
    {
        var document = new BoardDocument();
        var history = new CommandHistory();
        var tool = new ShapeCreationTool(document, CreateViewport(), history, TestEditing.NoSnap(), CreationDefaults.CreateRectangle);

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(300, 250));
        tool.PointerReleased(new PointD(300, 250));

        Assert.Single(document.Elements);

        history.Undo();
        Assert.Empty(document.Elements);

        history.Redo();
        var element = Assert.Single(document.Elements);
        Assert.Equal(200, element.Bounds.Width, Tolerance);
    }

    [Fact]
    public void DiscardedLineClick_RecordsNothing()
    {
        var document = new BoardDocument();
        var history = new CommandHistory();
        var tool = new LineCreationTool(document, CreateViewport(), history, TestEditing.NoSnap(), CreationDefaults.CreateLine);

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerReleased(new PointD(101, 100));

        Assert.Empty(document.Elements);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void PenStroke_IsOneUndoableEntry()
    {
        var document = new BoardDocument();
        var history = new CommandHistory();
        var tool = new PenTool(document, CreateViewport(), history);

        tool.PointerPressed(new PointD(0, 0));
        for (var i = 1; i <= 30; i++)
            tool.PointerMoved(new PointD(i * 4, i * 3));
        tool.PointerReleased(new PointD(120, 90));

        Assert.Single(document.Elements);
        Assert.Equal(1, history.Count);

        history.Undo();
        Assert.Empty(document.Elements);
    }

    [Fact]
    public void EraserGesture_IsUndoneAsASingleOperation()
    {
        var document = new BoardDocument();
        var strokes = new[]
        {
            CreateStroke(new PointD(50, 0), new PointD(50, 200)),
            CreateStroke(new PointD(150, 0), new PointD(150, 200)),
            CreateStroke(new PointD(250, 0), new PointD(250, 200)),
        };

        foreach (var stroke in strokes)
            document.AddElement(stroke);

        var history = new CommandHistory();
        var tool = new EraserTool(document, CreateViewport(), new SelectionState(), history);

        tool.PointerPressed(new PointD(0, 100));
        tool.PointerMoved(new PointD(300, 100));
        tool.PointerReleased(new PointD(300, 100));

        Assert.Empty(document.Elements);
        Assert.Equal(1, history.Count);

        history.Undo();

        Assert.Equal(strokes, document.Elements);
    }

    [Fact]
    public void EraserGesture_KeepsOrderAroundUntouchedElements()
    {
        var document = new BoardDocument();
        var keptFirst = new RectangleElement(new RectD(-500, -500, 10, 10));
        var strokeA = CreateStroke(new PointD(50, 0), new PointD(50, 200));
        var keptMiddle = new RectangleElement(new RectD(-400, -400, 10, 10));
        var strokeB = CreateStroke(new PointD(150, 0), new PointD(150, 200));
        var keptLast = new RectangleElement(new RectD(-300, -300, 10, 10));

        foreach (var element in new BoardElement[] { keptFirst, strokeA, keptMiddle, strokeB, keptLast })
            document.AddElement(element);

        var history = new CommandHistory();
        var tool = new EraserTool(document, CreateViewport(), new SelectionState(), history);

        tool.PointerPressed(new PointD(0, 100));
        tool.PointerMoved(new PointD(300, 100));
        tool.PointerReleased(new PointD(300, 100));

        Assert.Equal([keptFirst, keptMiddle, keptLast], document.Elements);

        history.Undo();

        Assert.Equal([keptFirst, strokeA, keptMiddle, strokeB, keptLast], document.Elements);
    }

    [Fact]
    public void EraserGestureWithoutHits_RecordsNothing()
    {
        var document = new BoardDocument();
        document.AddElement(CreateStroke(new PointD(500, 500), new PointD(600, 600)));
        var history = new CommandHistory();
        var tool = new EraserTool(document, CreateViewport(), new SelectionState(), history);

        tool.PointerPressed(new PointD(0, 0));
        tool.PointerMoved(new PointD(50, 0));
        tool.PointerReleased(new PointD(50, 0));

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void UndoingCreation_AlsoRemovesItFromHitTesting()
    {
        var document = new BoardDocument();
        var history = new CommandHistory();
        var creationTool = new ShapeCreationTool(document, CreateViewport(), history, TestEditing.NoSnap(), CreationDefaults.CreateEllipse);

        creationTool.PointerPressed(new PointD(100, 100));
        creationTool.PointerMoved(new PointD(300, 250));
        creationTool.PointerReleased(new PointD(300, 250));

        history.Undo();

        Assert.Null(document.GetTopmostElementAt(new PointD(200, 175)));
    }
}
