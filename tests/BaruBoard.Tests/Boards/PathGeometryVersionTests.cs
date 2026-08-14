using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Boards;

public class PathGeometryVersionTests
{
    private static PathElement CreatePath()
    {
        var path = new PathElement(new PointD(0, 0));
        path.AppendPoint(new PointD(50, 50));
        return path;
    }

    [Fact]
    public void AppendPoint_ChangesTheVersion()
    {
        var path = CreatePath();
        var before = path.GeometryVersion;

        path.AppendPoint(new PointD(100, 0));

        Assert.NotEqual(before, path.GeometryVersion);
    }

    [Fact]
    public void SetPoints_ChangesTheVersion()
    {
        var path = CreatePath();
        var before = path.GeometryVersion;

        path.SetPoints([new PointD(0, 0), new PointD(10, 10)]);

        Assert.NotEqual(before, path.GeometryVersion);
    }

    [Fact]
    public void MoveTo_ChangesTheVersion()
    {
        var path = CreatePath();
        var before = path.GeometryVersion;

        path.MoveTo(new PointD(500, 500));

        Assert.NotEqual(before, path.GeometryVersion);
    }

    [Fact]
    public void StrokeThickness_ChangesTheVersion()
    {
        var path = CreatePath();
        var before = path.GeometryVersion;

        path.StrokeThickness = 8;

        Assert.NotEqual(before, path.GeometryVersion);
    }

    [Fact]
    public void ReadingTheGeometry_DoesNotChangeTheVersion()
    {
        var path = CreatePath();
        var before = path.GeometryVersion;

        _ = path.Points;
        _ = path.Bounds;
        _ = path.Contains(new PointD(25, 25));

        Assert.Equal(before, path.GeometryVersion);
    }

    [Fact]
    public void CopiesTrackTheirOwnVersion()
    {
        var path = CreatePath();
        var copy = (PathElement)path.CreateCopy();
        var copyVersion = copy.GeometryVersion;

        path.AppendPoint(new PointD(200, 200));

        Assert.Equal(copyVersion, copy.GeometryVersion);
    }
}
