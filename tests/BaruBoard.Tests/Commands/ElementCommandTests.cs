using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Commands;

public class ElementCommandTests
{
    private const double Tolerance = 1e-9;

    private static RectangleElement CreateRectangle(double x = 0) => new(new RectD(x, 0, 50, 50));

    private static (BoardDocument Document, RectangleElement[] Elements) CreateDocument(int count)
    {
        var document = new BoardDocument();
        var elements = new RectangleElement[count];
        for (var i = 0; i < count; i++)
        {
            elements[i] = CreateRectangle(i * 100);
            document.AddElement(elements[i]);
        }

        return (document, elements);
    }

    [Fact]
    public void AddElementCommand_UndoRemovesAndRedoRestoresTheSameIndex()
    {
        var (document, elements) = CreateDocument(3);
        var added = CreateRectangle(999);
        document.InsertElement(1, added);
        var command = new AddElementCommand(document, added, 1);

        command.Undo();
        Assert.Equal([elements[0], elements[1], elements[2]], document.Elements);

        command.Execute();
        Assert.Equal([elements[0], added, elements[1], elements[2]], document.Elements);
    }

    [Fact]
    public void RemoveElementsCommand_RestoresSingleElementAtOriginalIndex()
    {
        var (document, elements) = CreateDocument(4);
        var target = elements[2];
        var index = document.IndexOf(target);
        document.RemoveElement(target);

        var command = new RemoveElementsCommand(document, [new RemovedElement(target, index)]);
        command.Undo();

        Assert.Equal([elements[0], elements[1], elements[2], elements[3]], document.Elements);
    }

    [Fact]
    public void RemoveElementsCommand_RestoresConsecutiveRemovalsInOrder()
    {
        // Removing B then C makes both report index 1 at removal time.
        var (document, elements) = CreateDocument(4);
        var removals = new List<RemovedElement>();
        foreach (var target in new[] { elements[1], elements[2] })
        {
            removals.Add(new RemovedElement(target, document.IndexOf(target)));
            document.RemoveElement(target);
        }

        Assert.Equal(1, removals[0].Index);
        Assert.Equal(1, removals[1].Index);

        var command = new RemoveElementsCommand(document, removals);
        command.Undo();

        Assert.Equal([elements[0], elements[1], elements[2], elements[3]], document.Elements);
    }

    [Fact]
    public void RemoveElementsCommand_RestoresSeparatedRemovals()
    {
        var (document, elements) = CreateDocument(5);
        var removals = new List<RemovedElement>();
        foreach (var target in new[] { elements[1], elements[3] })
        {
            removals.Add(new RemovedElement(target, document.IndexOf(target)));
            document.RemoveElement(target);
        }

        new RemoveElementsCommand(document, removals).Undo();

        Assert.Equal([elements[0], elements[1], elements[2], elements[3], elements[4]], document.Elements);
    }

    [Fact]
    public void RemoveElementsCommand_RestoresThreeOrMoreElements()
    {
        var (document, elements) = CreateDocument(6);
        var removals = new List<RemovedElement>();
        foreach (var target in new[] { elements[4], elements[0], elements[2], elements[3] })
        {
            removals.Add(new RemovedElement(target, document.IndexOf(target)));
            document.RemoveElement(target);
        }

        Assert.Equal([elements[1], elements[5]], document.Elements);

        new RemoveElementsCommand(document, removals).Undo();

        Assert.Equal(
            [elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]],
            document.Elements);
    }

    [Fact]
    public void RemoveElementsCommand_SurvivesUndoRedoUndo()
    {
        var (document, elements) = CreateDocument(4);
        var removals = new List<RemovedElement>();
        foreach (var target in new[] { elements[1], elements[2] })
        {
            removals.Add(new RemovedElement(target, document.IndexOf(target)));
            document.RemoveElement(target);
        }

        var command = new RemoveElementsCommand(document, removals);

        command.Undo();
        Assert.Equal([elements[0], elements[1], elements[2], elements[3]], document.Elements);

        command.Execute();
        Assert.Equal([elements[0], elements[3]], document.Elements);

        command.Undo();
        Assert.Equal([elements[0], elements[1], elements[2], elements[3]], document.Elements);
    }

    [Fact]
    public void RemoveElementsCommand_RejectsEmptyRemovals()
    {
        var document = new BoardDocument();

        Assert.Throws<ArgumentException>(() => new RemoveElementsCommand(document, []));
    }

    [Fact]
    public void MoveElementCommand_RestoresRectanglePosition()
    {
        var element = CreateRectangle(100);
        var before = element.Bounds.Position;
        element.MoveTo(new PointD(500, 300));
        var command = new MoveElementCommand(element, before, element.Bounds.Position);

        command.Undo();
        Assert.Equal(before, element.Bounds.Position);

        command.Execute();
        Assert.Equal(new PointD(500, 300), element.Bounds.Position);
    }

    [Fact]
    public void MoveElementCommand_RestoresLineEndpoints()
    {
        var line = new LineElement(new PointD(0, 0), new PointD(100, 50));
        var before = line.Bounds.Position;
        line.MoveTo(new PointD(before.X + 300, before.Y - 200));

        new MoveElementCommand(line, before, line.Bounds.Position).Undo();

        Assert.Equal(new PointD(0, 0), line.Start);
        Assert.Equal(new PointD(100, 50), line.End);
    }

    [Fact]
    public void MoveElementCommand_RestoresPathPoints()
    {
        var path = new PathElement(new PointD(0, 0));
        path.AppendPoint(new PointD(40, 60));
        var before = path.Bounds.Position;
        path.MoveTo(new PointD(before.X - 250, before.Y + 125));

        new MoveElementCommand(path, before, path.Bounds.Position).Undo();

        Assert.Equal(new PointD(0, 0), path.Points[0]);
        Assert.Equal(new PointD(40, 60), path.Points[1]);
    }

    [Fact]
    public void ResizeElementCommand_RestoresBounds()
    {
        var element = CreateRectangle();
        var before = element.Bounds;
        var after = new RectD(10, 10, 200, 120);
        element.ResizeTo(after);
        var command = new ResizeElementCommand(element, before, after);

        command.Undo();
        Assert.Equal(before, element.Bounds);

        command.Execute();
        Assert.Equal(after, element.Bounds);
    }

    [Fact]
    public void ChangeTextCommand_RestoresTextAndMeasuredSize()
    {
        var element = new TextElement(new PointD(10, 10), "before", 20);
        element.SetMeasuredSize(new SizeD(60, 22));
        var command = new ChangeTextCommand(
            element, "before", new SizeD(60, 22), "after text", new SizeD(140, 22));

        command.Execute();
        Assert.Equal("after text", element.Text);
        Assert.Equal(140, element.Bounds.Width, Tolerance);

        command.Undo();
        Assert.Equal("before", element.Text);
        Assert.Equal(60, element.Bounds.Width, Tolerance);
        Assert.Equal(10, element.Bounds.X, Tolerance);
    }
}
