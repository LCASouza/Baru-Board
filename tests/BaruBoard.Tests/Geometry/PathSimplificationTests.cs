using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class PathSimplificationTests
{
    [Fact]
    public void CollinearPoints_AreRemoved()
    {
        List<PointD> points = [new(0, 0), new(25, 0), new(50, 0), new(75, 0), new(100, 0)];

        var result = PathSimplification.Simplify(points, epsilon: 0.5);

        Assert.Equal(2, result.Count);
        Assert.Equal(new PointD(0, 0), result[0]);
        Assert.Equal(new PointD(100, 0), result[^1]);
    }

    [Fact]
    public void Corner_IsPreserved()
    {
        List<PointD> points = [new(0, 0), new(50, 0), new(50, 50)];

        var result = PathSimplification.Simplify(points, epsilon: 0.5);

        Assert.Equal(3, result.Count);
        Assert.Contains(new PointD(50, 0), result);
    }

    [Fact]
    public void EndpointsAreAlwaysPreserved()
    {
        List<PointD> points = [new(0, 0), new(10, 0.1), new(20, -0.1), new(30, 0.05), new(40, 0)];

        var result = PathSimplification.Simplify(points, epsilon: 1.0);

        Assert.Equal(new PointD(0, 0), result[0]);
        Assert.Equal(new PointD(40, 0), result[^1]);
    }

    [Fact]
    public void TwoOrFewerPoints_AreReturnedUnchanged()
    {
        List<PointD> two = [new(0, 0), new(10, 10)];
        List<PointD> one = [new(5, 5)];

        Assert.Equal(two, PathSimplification.Simplify(two, 1.0));
        Assert.Equal(one, PathSimplification.Simplify(one, 1.0));
    }

    [Fact]
    public void DeviationAboveEpsilon_KeepsThePoint()
    {
        List<PointD> points = [new(0, 0), new(50, 5), new(100, 0)];

        var kept = PathSimplification.Simplify(points, epsilon: 1.0);
        var removed = PathSimplification.Simplify(points, epsilon: 10.0);

        Assert.Equal(3, kept.Count);
        Assert.Equal(2, removed.Count);
    }

    [Fact]
    public void ZigZagAboveEpsilon_IsFullyPreserved()
    {
        List<PointD> points = [new(0, 0), new(10, 8), new(20, -8), new(30, 8), new(40, 0)];

        var result = PathSimplification.Simplify(points, epsilon: 1.0);

        Assert.Equal(5, result.Count);
    }
}
