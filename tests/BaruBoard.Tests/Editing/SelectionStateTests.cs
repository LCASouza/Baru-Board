using BaruBoard.Core.Boards;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Editing;

public class SelectionStateTests
{
    private const double Tolerance = 1e-9;

    private static RectangleElement CreateRectangle(double x = 0, double y = 0, double size = 10) =>
        new(new RectD(x, y, size, size));

    [Fact]
    public void NewSelection_IsEmpty()
    {
        var selection = new SelectionState();

        Assert.True(selection.IsEmpty);
        Assert.Equal(0, selection.Count);
        Assert.Null(selection.Primary);
        Assert.Null(selection.Bounds);
    }

    [Fact]
    public void Select_ReplacesTheWholeSelection()
    {
        var selection = new SelectionState();
        var first = CreateRectangle();
        var second = CreateRectangle(100);
        selection.Add(first);

        selection.Select(second);

        Assert.Equal([second], selection.Elements);
        Assert.Same(second, selection.Primary);
    }

    [Fact]
    public void Add_KeepsOrderAndNeverDuplicates()
    {
        var selection = new SelectionState();
        var first = CreateRectangle();
        var second = CreateRectangle(100);

        selection.Add(first);
        selection.Add(second);
        selection.Add(first);

        Assert.Equal(2, selection.Count);
        Assert.Same(first, selection.Primary);
        Assert.Equal([second, first], selection.Elements);
    }

    [Fact]
    public void Toggle_AddsThenRemoves()
    {
        var selection = new SelectionState();
        var element = CreateRectangle();

        selection.Toggle(element);
        Assert.True(selection.Contains(element));

        selection.Toggle(element);
        Assert.False(selection.Contains(element));
        Assert.Null(selection.Primary);
    }

    [Fact]
    public void RemovingThePrimary_PromotesTheLastRemaining()
    {
        var selection = new SelectionState();
        var first = CreateRectangle();
        var second = CreateRectangle(100);
        var third = CreateRectangle(200);
        selection.SelectMany([first, second, third]);

        selection.Remove(third);

        Assert.Same(second, selection.Primary);
    }

    [Fact]
    public void SelectMany_PreservesOrderAndIgnoresDuplicates()
    {
        var selection = new SelectionState();
        var first = CreateRectangle();
        var second = CreateRectangle(100);

        selection.SelectMany([first, second, first]);

        Assert.Equal([first, second], selection.Elements);
        Assert.Same(second, selection.Primary);
    }

    [Fact]
    public void Bounds_IsTheUnionOfTheSelectedElements()
    {
        var selection = new SelectionState();
        selection.SelectMany([CreateRectangle(0, 0, 10), CreateRectangle(90, 40, 10)]);

        var bounds = Assert.NotNull(selection.Bounds);
        Assert.Equal(0, bounds.Left, Tolerance);
        Assert.Equal(0, bounds.Top, Tolerance);
        Assert.Equal(100, bounds.Right, Tolerance);
        Assert.Equal(50, bounds.Bottom, Tolerance);
    }

    [Fact]
    public void RemoveMissing_DropsElementsNoLongerInTheDocument()
    {
        var document = new BoardDocument();
        var kept = CreateRectangle();
        var removed = CreateRectangle(100);
        document.AddElement(kept);
        document.AddElement(removed);

        var selection = new SelectionState();
        selection.SelectMany([kept, removed]);
        document.RemoveElement(removed);

        selection.RemoveMissing(document);

        Assert.Equal([kept], selection.Elements);
        Assert.Same(kept, selection.Primary);
    }

    [Fact]
    public void Clear_EmptiesTheSelection()
    {
        var selection = new SelectionState();
        selection.SelectMany([CreateRectangle(), CreateRectangle(50)]);

        selection.Clear();

        Assert.True(selection.IsEmpty);
        Assert.Null(selection.Primary);
    }
}
