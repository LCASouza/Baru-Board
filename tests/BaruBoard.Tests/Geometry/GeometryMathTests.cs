using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class GeometryMathTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void DistanceToSegment_PerpendicularProjection()
    {
        var distance = GeometryMath.DistanceToSegment(
            new PointD(50, 30), new PointD(0, 0), new PointD(100, 0));

        Assert.Equal(30, distance, Tolerance);
    }

    [Fact]
    public void DistanceToSegment_PointOnSegment_IsZero()
    {
        var distance = GeometryMath.DistanceToSegment(
            new PointD(25, 25), new PointD(0, 0), new PointD(100, 100));

        Assert.Equal(0, distance, Tolerance);
    }

    [Fact]
    public void DistanceToSegment_BeyondEnd_ClampsToEndpoint()
    {
        var distance = GeometryMath.DistanceToSegment(
            new PointD(140, 30), new PointD(0, 0), new PointD(100, 0));

        Assert.Equal(50, distance, Tolerance);
    }

    [Fact]
    public void DistanceToSegment_BeforeStart_ClampsToStartpoint()
    {
        var distance = GeometryMath.DistanceToSegment(
            new PointD(-30, -40), new PointD(0, 0), new PointD(100, 0));

        Assert.Equal(50, distance, Tolerance);
    }

    [Fact]
    public void DistanceToSegment_DegenerateSegment_IsDistanceToPoint()
    {
        var distance = GeometryMath.DistanceToSegment(
            new PointD(3, 4), new PointD(0, 0), new PointD(0, 0));

        Assert.Equal(5, distance, Tolerance);
    }

    [Fact]
    public void DistanceToSegment_NegativeCoordinates()
    {
        var distance = GeometryMath.DistanceToSegment(
            new PointD(-50, -20), new PointD(-100, -50), new PointD(0, -50));

        Assert.Equal(30, distance, Tolerance);
    }
}
