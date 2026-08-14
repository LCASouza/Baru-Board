using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class GridGeometryTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void DisplayStep_KeepsTheLogicalStepWhenItIsReadable()
    {
        Assert.Equal(20, GridGeometry.GetDisplayStep(20, 1.0), Tolerance);
        Assert.Equal(20, GridGeometry.GetDisplayStep(20, 4.0), Tolerance);
    }

    [Fact]
    public void DisplayStep_GrowsInMultiplesWhenZoomedOut()
    {
        var step = GridGeometry.GetDisplayStep(20, 0.1);

        Assert.True(step * 0.1 >= GridGeometry.MinScreenSpacing);
        Assert.Equal(0, step % 20, Tolerance);
    }

    [Fact]
    public void DisplayStep_IsAlwaysAMultipleOfTheLogicalStep()
    {
        foreach (var zoom in new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1, 2, 8 })
        {
            var step = GridGeometry.GetDisplayStep(20, zoom);
            Assert.Equal(0, step % 20, 1e-6);
        }
    }

    [Fact]
    public void GetLines_CoversTheRangeStartingOnAGridLine()
    {
        var lines = GridGeometry.GetLines(-35, 45, 20);

        Assert.Equal([-20.0, 0.0, 20.0, 40.0], lines);
    }

    [Fact]
    public void GetLines_OnAnEmptyOrInvertedRange_ReturnsNothing()
    {
        Assert.Empty(GridGeometry.GetLines(50, 10, 20));
        Assert.Empty(GridGeometry.GetLines(1, 19, 20));
    }

    [Fact]
    public void GetLines_IsBoundedAtExtremeZoom()
    {
        var lines = GridGeometry.GetLines(-1_000_000, 1_000_000, 1);

        Assert.Equal(GridGeometry.MaxLinesPerAxis, lines.Count);
    }

    [Fact]
    public void MajorLines_AppearEveryFifthStep()
    {
        Assert.True(GridGeometry.IsMajorLine(0, 20));
        Assert.True(GridGeometry.IsMajorLine(100, 20));
        Assert.True(GridGeometry.IsMajorLine(-100, 20));
        Assert.False(GridGeometry.IsMajorLine(20, 20));
        Assert.False(GridGeometry.IsMajorLine(80, 20));
    }
}
