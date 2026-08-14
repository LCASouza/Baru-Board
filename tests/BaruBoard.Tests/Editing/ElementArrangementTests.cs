using BaruBoard.Core.Boards;
using BaruBoard.Core.Commands;
using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Editing;

public class ElementArrangementTests
{
    private const double Tolerance = 1e-9;

    private static RectangleElement Rect(double x, double y, double width, double height) =>
        new(new RectD(x, y, width, height));

    private static void Apply(IReadOnlyList<ElementMove> moves) => new MoveElementsCommand(moves).Execute();

    [Fact]
    public void Align_NeedsAtLeastTwoElements()
    {
        Assert.Empty(ElementArrangement.Align([Rect(0, 0, 10, 10)], AlignmentMode.Left));
    }

    [Fact]
    public void AlignLeft_UsesTheSelectionBounds()
    {
        var elements = new BoardElement[] { Rect(10, 0, 40, 20), Rect(100, 50, 60, 20) };

        Apply(ElementArrangement.Align(elements, AlignmentMode.Left));

        Assert.Equal(10, elements[0].Bounds.Left, Tolerance);
        Assert.Equal(10, elements[1].Bounds.Left, Tolerance);
        Assert.Equal(50, elements[1].Bounds.Top, Tolerance);
    }

    [Fact]
    public void AlignRight_KeepsTheRightEdgesTogether()
    {
        var elements = new BoardElement[] { Rect(10, 0, 40, 20), Rect(100, 50, 60, 20) };

        Apply(ElementArrangement.Align(elements, AlignmentMode.Right));

        Assert.Equal(160, elements[0].Bounds.Right, Tolerance);
        Assert.Equal(160, elements[1].Bounds.Right, Tolerance);
    }

    [Fact]
    public void AlignHorizontalCenter_CentersInsideTheSelection()
    {
        var elements = new BoardElement[] { Rect(0, 0, 40, 20), Rect(60, 50, 40, 20) };

        Apply(ElementArrangement.Align(elements, AlignmentMode.HorizontalCenter));

        // The selection spans 0..100, so every element centers on 50.
        Assert.Equal(50, elements[0].Bounds.Left + elements[0].Bounds.Width / 2, Tolerance);
        Assert.Equal(50, elements[1].Bounds.Left + elements[1].Bounds.Width / 2, Tolerance);
        Assert.Equal(30, elements[0].Bounds.Left, Tolerance);
        Assert.Equal(50, elements[1].Bounds.Top, Tolerance);
    }

    [Fact]
    public void AlignTopMiddleAndBottom_WorkOnTheVerticalAxis()
    {
        var top = new BoardElement[] { Rect(0, 10, 20, 20), Rect(50, 100, 20, 40) };
        Apply(ElementArrangement.Align(top, AlignmentMode.Top));
        Assert.Equal(10, top[1].Bounds.Top, Tolerance);

        var middle = new BoardElement[] { Rect(0, 0, 20, 20), Rect(50, 0, 20, 100) };
        Apply(ElementArrangement.Align(middle, AlignmentMode.VerticalCenter));
        Assert.Equal(50, middle[0].Bounds.Top + middle[0].Bounds.Height / 2, Tolerance);

        var bottom = new BoardElement[] { Rect(0, 0, 20, 20), Rect(50, 0, 20, 100) };
        Apply(ElementArrangement.Align(bottom, AlignmentMode.Bottom));
        Assert.Equal(100, bottom[0].Bounds.Bottom, Tolerance);
    }

    [Fact]
    public void Align_WorksWithMixedElementTypes()
    {
        var line = new LineElement(new PointD(200, 200), new PointD(260, 240)) { StrokeThickness = 2 };
        var elements = new BoardElement[] { Rect(0, 0, 40, 20), line };

        Apply(ElementArrangement.Align(elements, AlignmentMode.Left));

        Assert.Equal(0, line.Bounds.Left, Tolerance);
        Assert.Equal(60, line.End.X - line.Start.X, Tolerance);
    }

    [Fact]
    public void Distribute_NeedsAtLeastThreeElements()
    {
        Assert.Empty(ElementArrangement.Distribute(
            [Rect(0, 0, 10, 10), Rect(50, 0, 10, 10)],
            DistributionMode.Horizontal));
    }

    [Fact]
    public void DistributeHorizontally_ProducesEqualGapsBetweenBounds()
    {
        var elements = new BoardElement[]
        {
            Rect(0, 0, 20, 10),
            Rect(30, 0, 60, 10),
            Rect(120, 0, 40, 10),
            Rect(300, 0, 20, 10),
        };

        Apply(ElementArrangement.Distribute(elements, DistributionMode.Horizontal));

        var ordered = elements.OrderBy(element => element.Bounds.Left).ToList();
        var gaps = new List<double>();
        for (var i = 1; i < ordered.Count; i++)
            gaps.Add(ordered[i].Bounds.Left - ordered[i - 1].Bounds.Right);

        Assert.All(gaps, gap => Assert.Equal(gaps[0], gap, 1e-6));
        Assert.Equal(0, ordered[0].Bounds.Left, Tolerance);
        Assert.Equal(320, ordered[^1].Bounds.Right, Tolerance);
    }

    [Fact]
    public void DistributeVertically_ProducesEqualGapsBetweenBounds()
    {
        var elements = new BoardElement[]
        {
            Rect(0, 0, 10, 20),
            Rect(0, 25, 10, 80),
            Rect(0, 400, 10, 20),
        };

        Apply(ElementArrangement.Distribute(elements, DistributionMode.Vertical));

        var ordered = elements.OrderBy(element => element.Bounds.Top).ToList();
        var firstGap = ordered[1].Bounds.Top - ordered[0].Bounds.Bottom;
        var secondGap = ordered[2].Bounds.Top - ordered[1].Bounds.Bottom;

        Assert.Equal(firstGap, secondGap, 1e-6);
        Assert.Equal(0, ordered[0].Bounds.Top, Tolerance);
        Assert.Equal(420, ordered[^1].Bounds.Bottom, Tolerance);
    }

    [Fact]
    public void Distribute_LeavesTheOutermostElementsUntouched()
    {
        var first = Rect(0, 0, 10, 10);
        var middle = Rect(20, 0, 10, 10);
        var last = Rect(200, 0, 10, 10);

        var moves = ElementArrangement.Distribute([first, middle, last], DistributionMode.Horizontal);

        Assert.Single(moves);
        Assert.Same(middle, moves[0].Element);
    }

    [Fact]
    public void AlignmentAndDistribution_AreSingleHistoryEntries()
    {
        var history = new CommandHistory();
        var selection = new SelectionState();
        selection.SelectMany([Rect(0, 0, 10, 10), Rect(50, 30, 10, 10), Rect(200, 60, 10, 10)]);

        Assert.True(EditingOperations.Align(selection, history, AlignmentMode.Top));
        Assert.True(EditingOperations.Distribute(selection, history, DistributionMode.Horizontal));

        Assert.Equal(2, history.Count);

        history.Undo();
        history.Undo();

        Assert.Equal(30, selection.Elements[1].Bounds.Top, Tolerance);
        Assert.Equal(50, selection.Elements[1].Bounds.Left, Tolerance);
    }

    [Fact]
    public void AlignmentWithNothingToDo_DoesNotTouchTheHistory()
    {
        var history = new CommandHistory();
        var selection = new SelectionState();
        selection.SelectMany([Rect(0, 0, 10, 10), Rect(0, 50, 10, 10)]);

        Assert.False(EditingOperations.Align(selection, history, AlignmentMode.Left));
        Assert.False(history.CanUndo);
    }
}
