using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class RectDTests
{
    [Fact]
    public void Intersects_OverlappingRectangles_ReturnsTrue()
    {
        var a = new RectD(0, 0, 100, 100);
        var b = new RectD(50, 50, 100, 100);

        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_ContainedRectangle_ReturnsTrue()
    {
        var outer = new RectD(0, 0, 100, 100);
        var inner = new RectD(25, 25, 10, 10);

        Assert.True(outer.Intersects(inner));
        Assert.True(inner.Intersects(outer));
    }

    [Fact]
    public void Intersects_DisjointRectangles_ReturnsFalse()
    {
        var a = new RectD(0, 0, 100, 100);
        var b = new RectD(200, 200, 50, 50);

        Assert.False(a.Intersects(b));
        Assert.False(b.Intersects(a));
    }

    [Fact]
    public void Intersects_SharedEdge_ReturnsTrue()
    {
        var a = new RectD(0, 0, 100, 100);
        var b = new RectD(100, 0, 50, 100);

        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_SharedCorner_ReturnsTrue()
    {
        var a = new RectD(0, 0, 100, 100);
        var b = new RectD(100, 100, 50, 50);

        Assert.True(a.Intersects(b));
    }

    [Fact]
    public void Intersects_NegativeCoordinates_Works()
    {
        var a = new RectD(-200, -150, 100, 100);
        var b = new RectD(-150, -100, 100, 100);
        var c = new RectD(50, 50, 10, 10);

        Assert.True(a.Intersects(b));
        Assert.False(a.Intersects(c));
    }

    [Theory]
    [InlineData(50, 50, true)]
    [InlineData(0, 0, true)]
    [InlineData(100, 100, true)]
    [InlineData(100, 50, true)]
    [InlineData(101, 50, false)]
    [InlineData(-1, 50, false)]
    [InlineData(50, -0.001, false)]
    public void Contains_TreatsEdgesAsInside(double pointX, double pointY, bool expected)
    {
        var rect = new RectD(0, 0, 100, 100);

        Assert.Equal(expected, rect.Contains(new PointD(pointX, pointY)));
    }

    [Fact]
    public void EdgeProperties_DeriveFromPositionAndSize()
    {
        var rect = new RectD(-30, 40, 200, 100);

        Assert.Equal(-30, rect.Left);
        Assert.Equal(40, rect.Top);
        Assert.Equal(170, rect.Right);
        Assert.Equal(140, rect.Bottom);
        Assert.Equal(new PointD(-30, 40), rect.Position);
        Assert.Equal(new SizeD(200, 100), rect.Size);
    }
}
