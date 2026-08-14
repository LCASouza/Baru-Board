using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;
using BaruBoard.Core.Tools;
using BaruBoard.Core.Viewports;

namespace BaruBoard.Tests.Tools;

public class CreationToolTests
{
    private const double Tolerance = 1e-9;

    private static Viewport CreateViewport(double zoom = 1.0, double positionX = 0, double positionY = 0) => new()
    {
        Position = new PointD(positionX, positionY),
        Zoom = zoom,
        ViewportSize = new SizeD(1200, 800),
    };

    [Fact]
    public void ShapeTool_DragCreatesElementWithDraggedBounds()
    {
        var document = new BoardDocument();
        var tool = new ShapeCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateRectangle);

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(300, 250));
        tool.PointerReleased(new PointD(300, 250));

        var element = Assert.IsType<RectangleElement>(Assert.Single(document.Elements));
        Assert.Equal(100, element.Bounds.X, Tolerance);
        Assert.Equal(100, element.Bounds.Y, Tolerance);
        Assert.Equal(200, element.Bounds.Width, Tolerance);
        Assert.Equal(150, element.Bounds.Height, Tolerance);
    }

    [Fact]
    public void ShapeTool_InvertedDrag_NormalizesBounds()
    {
        var document = new BoardDocument();
        var tool = new ShapeCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateEllipse);

        tool.PointerPressed(new PointD(300, 250));
        tool.PointerMoved(new PointD(100, 100));
        tool.PointerReleased(new PointD(100, 100));

        var element = Assert.Single(document.Elements);
        Assert.Equal(100, element.Bounds.X, Tolerance);
        Assert.Equal(100, element.Bounds.Y, Tolerance);
        Assert.Equal(200, element.Bounds.Width, Tolerance);
        Assert.Equal(150, element.Bounds.Height, Tolerance);
    }

    [Fact]
    public void ShapeTool_PlainClick_CreatesDefaultSizeCenteredOnClick()
    {
        var document = new BoardDocument();
        var tool = new ShapeCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateRectangle);

        tool.PointerPressed(new PointD(400, 300));
        tool.PointerReleased(new PointD(401, 300));

        var element = Assert.Single(document.Elements);
        Assert.Equal(CreationDefaults.ShapeSize.Width, element.Bounds.Width, Tolerance);
        Assert.Equal(CreationDefaults.ShapeSize.Height, element.Bounds.Height, Tolerance);
        Assert.Equal(400, element.Bounds.X + element.Bounds.Width / 2, Tolerance);
        Assert.Equal(300, element.Bounds.Y + element.Bounds.Height / 2, Tolerance);
    }

    [Fact]
    public void ShapeTool_ConvertsScreenToWorldThroughViewport()
    {
        var document = new BoardDocument();
        var tool = new ShapeCreationTool(document, CreateViewport(zoom: 2.0, positionX: 100, positionY: 50), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateRectangle);

        tool.PointerPressed(new PointD(200, 100));
        tool.PointerMoved(new PointD(400, 300));
        tool.PointerReleased(new PointD(400, 300));

        var element = Assert.Single(document.Elements);
        Assert.Equal(200, element.Bounds.X, Tolerance);
        Assert.Equal(100, element.Bounds.Y, Tolerance);
        Assert.Equal(100, element.Bounds.Width, Tolerance);
        Assert.Equal(100, element.Bounds.Height, Tolerance);
    }

    [Fact]
    public void ShapeTool_RaisesCreationCompleted()
    {
        var document = new BoardDocument();
        var tool = new ShapeCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateRectangle);
        BoardElement? completed = null;
        tool.CreationCompleted += element => completed = element;

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(200, 200));
        tool.PointerReleased(new PointD(200, 200));

        Assert.NotNull(completed);
        Assert.Same(document.Elements[0], completed);
    }

    [Fact]
    public void ShapeTool_TinyDrag_EnforcesMinimumSize()
    {
        var document = new BoardDocument();
        var tool = new ShapeCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateRectangle);

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(105, 102));
        tool.PointerReleased(new PointD(105, 102));

        var element = Assert.Single(document.Elements);
        Assert.True(element.Bounds.Width >= SelectionGeometry.MinElementSize);
        Assert.True(element.Bounds.Height >= SelectionGeometry.MinElementSize);
    }

    [Fact]
    public void LineTool_DragSetsEndpoints()
    {
        var document = new BoardDocument();
        var tool = new LineCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateLine);

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerMoved(new PointD(340, 260));
        tool.PointerReleased(new PointD(340, 260));

        var line = Assert.IsType<LineElement>(Assert.Single(document.Elements));
        Assert.Equal(new PointD(100, 100), line.Start);
        Assert.Equal(new PointD(340, 260), line.End);
    }

    [Fact]
    public void LineTool_PlainClick_DiscardsLine()
    {
        var document = new BoardDocument();
        var tool = new LineCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateLine);
        var completedRaised = false;
        tool.CreationCompleted += _ => completedRaised = true;

        tool.PointerPressed(new PointD(100, 100));
        tool.PointerReleased(new PointD(101, 101));

        Assert.Empty(document.Elements);
        Assert.False(completedRaised);
    }

    [Fact]
    public void LineTool_WithArrowFactory_CreatesArrow()
    {
        var document = new BoardDocument();
        var tool = new LineCreationTool(document, CreateViewport(), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateArrow);

        tool.PointerPressed(new PointD(0, 0));
        tool.PointerMoved(new PointD(200, 0));
        tool.PointerReleased(new PointD(200, 0));

        Assert.IsType<ArrowElement>(Assert.Single(document.Elements));
    }

    [Fact]
    public void LineTool_ConvertsThroughViewportZoom()
    {
        var document = new BoardDocument();
        var tool = new LineCreationTool(document, CreateViewport(zoom: 0.5), new CommandHistory(), TestEditing.NoSnap(), CreationDefaults.CreateLine);

        tool.PointerPressed(new PointD(50, 50));
        tool.PointerMoved(new PointD(150, 100));
        tool.PointerReleased(new PointD(150, 100));

        var line = Assert.IsType<LineElement>(Assert.Single(document.Elements));
        Assert.Equal(new PointD(100, 100), line.Start);
        Assert.Equal(new PointD(300, 200), line.End);
    }

    [Fact]
    public void TextTool_CreatesElementAndRequestsEdit()
    {
        var document = new BoardDocument();
        var tool = new TextTool(document, CreateViewport(zoom: 2.0));
        TextElement? requested = null;
        tool.EditRequested += element => requested = element;

        tool.PointerPressed(new PointD(200, 100));

        var text = Assert.IsType<TextElement>(Assert.Single(document.Elements));
        Assert.Same(text, requested);
        Assert.Equal(100, text.Bounds.X, Tolerance);
        Assert.Equal(50, text.Bounds.Y, Tolerance);
        Assert.Equal(CreationDefaults.DefaultFontSize, text.FontSize, Tolerance);
    }

    [Fact]
    public void SelectionTool_NonResizableElement_ShowsNoResizeCursorOnBoundsCorner()
    {
        var document = new BoardDocument();
        var line = new LineElement(new PointD(100, 100), new PointD(300, 250)) { StrokeThickness = 2 };
        document.AddElement(line);
        var viewport = CreateViewport();
        var selection = new SelectionState();
        var tool = new SelectionTool(document, viewport, selection, new CommandHistory(), TestEditing.NoSnap(), new EditorInteractionState());

        tool.PointerPressed(new PointD(200, 175));
        tool.PointerReleased(new PointD(200, 175));
        Assert.Same(line, selection.Primary);

        // Bounds corner would be the BottomRight handle if the element were resizable.
        tool.PointerMoved(new PointD(301, 251));

        Assert.NotEqual(EditorCursor.ResizeNwSe, tool.Cursor);
    }

    [Fact]
    public void SelectionTool_HitsLineUsingScreenSpaceTolerance()
    {
        var document = new BoardDocument();
        var line = new LineElement(new PointD(0, 0), new PointD(200, 0)) { StrokeThickness = 2 };
        document.AddElement(line);
        var viewport = CreateViewport(zoom: 0.5);
        var selection = new SelectionState();
        var tool = new SelectionTool(document, viewport, selection, new CommandHistory(), TestEditing.NoSnap(), new EditorInteractionState());

        // Screen (50, 3) is world (100, 6): 5 world units off the line body, but
        // within the 4-DIP tolerance at zoom 0.5 (8 world units).
        tool.PointerPressed(new PointD(50, 3));

        Assert.Same(line, selection.Primary);
    }
}
