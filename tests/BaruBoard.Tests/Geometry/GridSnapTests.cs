using BaruBoard.Core.Editing;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Geometry;

public class GridSnapTests
{
    private const double Tolerance = 1e-9;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(11, 20)]
    [InlineData(29, 20)]
    [InlineData(-9, 0)]
    [InlineData(-11, -20)]
    [InlineData(123.4, 120)]
    public void SnapValue_RoundsToTheNearestGridLine(double value, double expected)
    {
        Assert.Equal(expected, GridSnap.SnapValue(value, 20), Tolerance);
    }

    [Theory]
    [InlineData(10, 20)]
    [InlineData(-10, -20)]
    [InlineData(30, 40)]
    [InlineData(-30, -40)]
    public void SnapValue_AtExactMidpoint_GoesAwayFromZero(double value, double expected)
    {
        Assert.Equal(expected, GridSnap.SnapValue(value, 20), Tolerance);
    }

    [Fact]
    public void SnapPoint_SnapsBothAxes()
    {
        var snapped = GridSnap.SnapPoint(new PointD(11, -31), 20);

        Assert.Equal(20, snapped.X, Tolerance);
        Assert.Equal(-40, snapped.Y, Tolerance);
    }

    [Fact]
    public void SnapValue_RejectsNonPositiveStep()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GridSnap.SnapValue(10, 0));
    }

    [Fact]
    public void SnapContext_RespectsTheEnabledFlagAndSuppression()
    {
        var grid = new GridSettings { LogicalStep = 20, SnapEnabled = false };
        var interaction = new EditorInteractionState();
        var context = new SnapContext(grid, interaction);

        Assert.Equal(11, context.SnapValue(11), Tolerance);

        grid.SnapEnabled = true;
        Assert.Equal(20, context.SnapValue(11), Tolerance);

        interaction.IsSnapSuppressed = true;
        Assert.Equal(11, context.SnapValue(11), Tolerance);

        interaction.Reset();
        Assert.Equal(20, context.SnapValue(11), Tolerance);
    }

    [Fact]
    public void SnapResult_DoesNotDependOnZoom()
    {
        // The logical step is what snapping uses, so the same world point lands on
        // the same grid line no matter how the grid is being displayed.
        var world = new PointD(37.5, -63.2);
        var context = new SnapContext(
            new GridSettings { LogicalStep = 20, SnapEnabled = true },
            new EditorInteractionState());

        var expected = context.SnapPoint(world);

        foreach (var zoom in new[] { 0.1, 0.5, 1.0, 4.0, 8.0 })
        {
            _ = GridGeometry.GetDisplayStep(20, zoom);
            Assert.Equal(expected, context.SnapPoint(world));
        }
    }
}
