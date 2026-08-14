using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class CullingTests
{
    private static readonly RectD VisibleBounds = new(0, 0, 1000, 800);

    private static RectangleElement CreateRectangle(double x, double y, double width, double height) =>
        new(new RectD(x, y, width, height));

    private static BoardDocument CreateDocument(params BoardElement[] elements)
    {
        var document = new BoardDocument();
        foreach (var element in elements)
        {
            document.AddElement(element);
        }

        return document;
    }

    [Fact]
    public void ElementFullyInside_IsReturned()
    {
        var element = CreateRectangle(100, 100, 200, 150);
        var document = CreateDocument(element);

        var visible = document.GetElementsIntersecting(VisibleBounds);

        Assert.Contains(element, visible);
    }

    [Fact]
    public void ElementPartiallyInside_IsReturned()
    {
        var element = CreateRectangle(-50, -50, 100, 100);
        var document = CreateDocument(element);

        var visible = document.GetElementsIntersecting(VisibleBounds);

        Assert.Contains(element, visible);
    }

    [Fact]
    public void ElementFullyOutside_IsNotReturned()
    {
        var element = CreateRectangle(2000, 2000, 50, 50);
        var document = CreateDocument(element);

        var visible = document.GetElementsIntersecting(VisibleBounds);

        Assert.Empty(visible);
    }

    [Fact]
    public void ElementTouchingViewportEdge_IsReturned()
    {
        var touchingRight = CreateRectangle(1000, 100, 50, 50);
        var touchingBottom = CreateRectangle(100, 800, 50, 50);
        var document = CreateDocument(touchingRight, touchingBottom);

        var visible = document.GetElementsIntersecting(VisibleBounds).ToList();

        Assert.Equal(2, visible.Count);
    }

    [Fact]
    public void CullingWorksInNegativeCoordinateSpace()
    {
        var visibleBounds = new RectD(-500, -400, 400, 300);
        var inside = CreateRectangle(-350, -300, 100, 100);
        var outside = CreateRectangle(10, 10, 5, 5);
        var document = CreateDocument(inside, outside);

        var visible = document.GetElementsIntersecting(visibleBounds).ToList();

        Assert.Single(visible);
        Assert.Contains(inside, visible);
    }

    [Fact]
    public void MixedScenario_ReturnsExactlyTheIntersectingElements()
    {
        var fullyInside = CreateRectangle(400, 300, 100, 100);
        var partiallyInside = CreateRectangle(950, 750, 200, 200);
        var farAway = CreateRectangle(-3000, -2000, 100, 100);
        var huge = CreateRectangle(-5000, -5000, 10000, 10000);
        var document = CreateDocument(fullyInside, partiallyInside, farAway, huge);

        var visibleIds = document.GetElementsIntersecting(VisibleBounds)
            .Select(e => e.Id)
            .ToHashSet();

        Assert.Equal(3, visibleIds.Count);
        Assert.Contains(fullyInside.Id, visibleIds);
        Assert.Contains(partiallyInside.Id, visibleIds);
        Assert.Contains(huge.Id, visibleIds);
        Assert.DoesNotContain(farAway.Id, visibleIds);
    }
}
