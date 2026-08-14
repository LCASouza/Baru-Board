using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class BoardDocumentOrderTests
{
    private static RectangleElement CreateRectangle(double x) => new(new RectD(x, 0, 50, 50));

    [Fact]
    public void IndexOf_ReturnsPositionOrMinusOne()
    {
        var document = new BoardDocument();
        var first = CreateRectangle(0);
        var second = CreateRectangle(100);
        document.AddElement(first);
        document.AddElement(second);

        Assert.Equal(0, document.IndexOf(first));
        Assert.Equal(1, document.IndexOf(second));
        Assert.Equal(-1, document.IndexOf(CreateRectangle(200)));
    }

    [Fact]
    public void InsertElement_PlacesElementAtGivenPosition()
    {
        var document = new BoardDocument();
        var a = CreateRectangle(0);
        var c = CreateRectangle(200);
        document.AddElement(a);
        document.AddElement(c);

        var b = CreateRectangle(100);
        document.InsertElement(1, b);

        Assert.Equal([a, b, c], document.Elements);
    }

    [Fact]
    public void InsertElement_AtCount_AppendsToTheEnd()
    {
        var document = new BoardDocument();
        var a = CreateRectangle(0);
        document.AddElement(a);

        var b = CreateRectangle(100);
        document.InsertElement(document.Elements.Count, b);

        Assert.Equal([a, b], document.Elements);
    }

    [Fact]
    public void InsertElement_OutOfRange_Throws()
    {
        var document = new BoardDocument();
        document.AddElement(CreateRectangle(0));

        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertElement(5, CreateRectangle(100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertElement(-1, CreateRectangle(100)));
    }

    [Fact]
    public void RestoringAtOriginalIndex_KeepsHitTestingTieBreak()
    {
        var document = new BoardDocument();
        var bottom = CreateRectangle(0);
        var middle = new RectangleElement(new RectD(0, 0, 50, 50));
        var top = new RectangleElement(new RectD(0, 0, 50, 50));
        document.AddElement(bottom);
        document.AddElement(middle);
        document.AddElement(top);

        var index = document.IndexOf(middle);
        document.RemoveElement(middle);
        document.InsertElement(index, middle);

        Assert.Same(top, document.GetTopmostElementAt(new PointD(25, 25)));
    }
}
