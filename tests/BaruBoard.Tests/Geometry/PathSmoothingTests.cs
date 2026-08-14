using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class PathSmoothingTests
{
    [Fact]
    public void FewerThanTwoPoints_ProducesNoSegments()
    {
        Assert.Empty(PathSmoothing.GetSegments([new PointD(1, 1)]));
        Assert.Empty(PathSmoothing.GetSegments([]));
    }

    [Fact]
    public void TwoPoints_ProduceSingleStraightSegment()
    {
        var segments = PathSmoothing.GetSegments([new PointD(0, 0), new PointD(100, 0)]);

        var segment = Assert.Single(segments);
        Assert.Equal(new PointD(50, 0), segment.Control);
        Assert.Equal(new PointD(100, 0), segment.End);
    }

    [Fact]
    public void ThreePoints_ProduceOneCurveEndingOnLastPoint()
    {
        var segments = PathSmoothing.GetSegments([new PointD(0, 0), new PointD(50, 50), new PointD(100, 0)]);

        var segment = Assert.Single(segments);
        Assert.Equal(new PointD(50, 50), segment.Control);
        Assert.Equal(new PointD(100, 0), segment.End);
    }

    [Fact]
    public void FourPoints_UseMidpointsBetweenInteriorPoints()
    {
        var segments = PathSmoothing.GetSegments(
            [new PointD(0, 0), new PointD(40, 40), new PointD(80, 40), new PointD(120, 0)]);

        Assert.Equal(2, segments.Count);
        Assert.Equal(new PointD(40, 40), segments[0].Control);
        Assert.Equal(new PointD(60, 40), segments[0].End);
        Assert.Equal(new PointD(80, 40), segments[1].Control);
        Assert.Equal(new PointD(120, 0), segments[1].End);
    }

    [Fact]
    public void LastSegment_AlwaysEndsOnFinalPoint()
    {
        List<PointD> points = [new(0, 0), new(10, 5), new(20, -5), new(30, 5), new(40, 0)];

        var segments = PathSmoothing.GetSegments(points);

        Assert.Equal(points[^1], segments[^1].End);
    }
}
