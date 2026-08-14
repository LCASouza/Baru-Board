using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class ArrowGeometryTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void GetHeadPoints_HorizontalArrow_ProducesSymmetricWings()
    {
        var (left, right) = ArrowGeometry.GetHeadPoints(
            new PointD(0, 0), new PointD(100, 0), headLength: 10, headAngle: Math.PI / 6);

        var expectedX = 100 - 10 * Math.Cos(Math.PI / 6);

        Assert.Equal(expectedX, left.X, Tolerance);
        Assert.Equal(-5, left.Y, Tolerance);
        Assert.Equal(expectedX, right.X, Tolerance);
        Assert.Equal(5, right.Y, Tolerance);
    }

    [Fact]
    public void GetHeadPoints_WingsAreAtHeadLengthFromTip()
    {
        var end = new PointD(80, -40);
        var (left, right) = ArrowGeometry.GetHeadPoints(
            new PointD(-20, 30), end, headLength: 12, headAngle: Math.PI / 6);

        Assert.Equal(12, (left - end).Length, Tolerance);
        Assert.Equal(12, (right - end).Length, Tolerance);
    }

    [Fact]
    public void GetHeadPoints_DegenerateArrow_ReturnsTip()
    {
        var point = new PointD(10, 10);

        var (left, right) = ArrowGeometry.GetHeadPoints(point, point, 10, Math.PI / 6);

        Assert.Equal(point, left);
        Assert.Equal(point, right);
    }

    [Fact]
    public void ArrowBounds_IncludeHeadWings()
    {
        var line = new LineElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 2 };
        var arrow = new ArrowElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 2 };

        // The wings extend vertically beyond the segment, so the arrow's bounds
        // must be taller than the plain line's.
        Assert.True(arrow.Bounds.Height > line.Bounds.Height);
        Assert.True(arrow.Bounds.Top < line.Bounds.Top);
        Assert.True(arrow.Bounds.Bottom > line.Bounds.Bottom);
    }

    [Fact]
    public void ArrowContains_PointOnWing_ReturnsTrue()
    {
        var arrow = new ArrowElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 2 };
        var (left, _) = ArrowGeometry.GetHeadPoints(
            arrow.Start, arrow.End, arrow.HeadLength, ArrowGeometry.DefaultHeadAngle);

        Assert.True(arrow.Contains(left));
    }

    [Fact]
    public void ArrowContains_PointFarFromLineAndHead_ReturnsFalse()
    {
        var arrow = new ArrowElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 2 };

        Assert.False(arrow.Contains(new PointD(50, 30)));
    }

    [Fact]
    public void ArrowMoveTo_KeepsHeadGeometryConsistent()
    {
        var arrow = new ArrowElement(new PointD(0, 0), new PointD(100, 0)) { StrokeThickness = 2 };
        var heightBefore = arrow.Bounds.Height;

        arrow.MoveTo(new PointD(arrow.Bounds.X + 500, arrow.Bounds.Y - 300));

        Assert.Equal(heightBefore, arrow.Bounds.Height, Tolerance);
        Assert.Equal(600, arrow.End.X, Tolerance);
    }
}
