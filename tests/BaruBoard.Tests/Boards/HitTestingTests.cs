using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class HitTestingTests
{
    private static RectangleElement CreateRectangle(double x, double y, double width, double height, int zIndex = 0) =>
        new(new RectD(x, y, width, height)) { ZIndex = zIndex };

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
    public void ReturnsElementUnderPoint()
    {
        var element = CreateRectangle(100, 100, 200, 150);
        var document = CreateDocument(element);

        var hit = document.GetTopmostElementAt(new PointD(150, 120));

        Assert.Same(element, hit);
    }

    [Fact]
    public void ReturnsNullWhenNothingIsHit()
    {
        var document = CreateDocument(CreateRectangle(100, 100, 200, 150));

        var hit = document.GetTopmostElementAt(new PointD(500, 500));

        Assert.Null(hit);
    }

    [Fact]
    public void OverlappingElements_HigherZIndexWins()
    {
        var below = CreateRectangle(0, 0, 200, 200, zIndex: 0);
        var above = CreateRectangle(50, 50, 200, 200, zIndex: 5);
        var document = CreateDocument(below, above);

        var hit = document.GetTopmostElementAt(new PointD(100, 100));

        Assert.Same(above, hit);
    }

    [Fact]
    public void OverlappingElements_HigherZIndexWins_EvenWhenAddedFirst()
    {
        var above = CreateRectangle(0, 0, 200, 200, zIndex: 5);
        var below = CreateRectangle(50, 50, 200, 200, zIndex: 1);
        var document = CreateDocument(above, below);

        var hit = document.GetTopmostElementAt(new PointD(100, 100));

        Assert.Same(above, hit);
    }

    [Fact]
    public void OverlappingElements_SameZIndex_LastAddedWins()
    {
        var first = CreateRectangle(0, 0, 200, 200);
        var second = CreateRectangle(50, 50, 200, 200);
        var document = CreateDocument(first, second);

        var hit = document.GetTopmostElementAt(new PointD(100, 100));

        Assert.Same(second, hit);
    }

    [Fact]
    public void WorksWithNegativeCoordinates()
    {
        var element = CreateRectangle(-500, -400, 200, 150);
        var document = CreateDocument(element);

        Assert.Same(element, document.GetTopmostElementAt(new PointD(-450, -350)));
        Assert.Null(document.GetTopmostElementAt(new PointD(-250, -350)));
    }

    [Fact]
    public void PointOnElementEdge_CountsAsHit()
    {
        var element = CreateRectangle(100, 100, 200, 150);
        var document = CreateDocument(element);

        var hit = document.GetTopmostElementAt(new PointD(300, 250));

        Assert.Same(element, hit);
    }
}
