using BaruBoard.Core.Exporting;
using BaruBoard.Core.Geometry;

namespace BaruBoard.Tests.Exporting;

public class ExportGeometryTests
{
    private const double Tolerance = 1e-9;

    private static ExportPlan Plan(RectD region, double scale, double margin = 0) =>
        ExportGeometry.CreatePlan(region, scale, margin);

    [Theory]
    [InlineData(1.0, 400, 300)]
    [InlineData(2.0, 800, 600)]
    [InlineData(3.0, 1200, 900)]
    public void OneWorldUnitBecomesScaleOutputPixels(double scale, int expectedWidth, int expectedHeight)
    {
        var plan = Plan(new RectD(0, 0, 400, 300), scale);

        Assert.Equal(expectedWidth, plan.PixelWidth);
        Assert.Equal(expectedHeight, plan.PixelHeight);
        Assert.Equal(scale, plan.EffectiveScale, Tolerance);
        Assert.False(plan.WasScaleReduced);
    }

    [Fact]
    public void NegativeCoordinates_ArePreservedInTheRegion()
    {
        var plan = Plan(new RectD(-2000.5, -1500.25, 400, 300), 2.0);

        Assert.Equal(-2000.5, plan.WorldRegion.X, Tolerance);
        Assert.Equal(-1500.25, plan.WorldRegion.Y, Tolerance);
        Assert.Equal(800, plan.PixelWidth);
    }

    [Fact]
    public void MarginIsExpressedInOutputPixels()
    {
        var withoutMargin = Plan(new RectD(0, 0, 400, 300), 2.0);
        var withMargin = Plan(new RectD(0, 0, 400, 300), 2.0, margin: 24);

        // 24 output pixels at 2x is 12 world units on each side.
        Assert.Equal(withoutMargin.PixelWidth + 48, withMargin.PixelWidth);
        Assert.Equal(-12, withMargin.WorldRegion.X, Tolerance);
    }

    [Fact]
    public void WideRegion_IsClampedByTheDimensionLimit()
    {
        var plan = Plan(new RectD(0, 0, 50_000, 100), 1.0);

        Assert.True(plan.WasScaleReduced);
        Assert.Equal(ExportSettings.MaxDimension, plan.PixelWidth);
        Assert.True(plan.PixelHeight >= 1);
    }

    [Fact]
    public void TallRegion_IsClampedByTheDimensionLimit()
    {
        var plan = Plan(new RectD(0, 0, 100, 50_000), 1.0);

        Assert.True(plan.WasScaleReduced);
        Assert.Equal(ExportSettings.MaxDimension, plan.PixelHeight);
    }

    [Fact]
    public void LargeSquareRegion_IsClampedByThePixelBudget()
    {
        // 6000 x 6000 at 3x would be 324 megapixels.
        var plan = Plan(new RectD(0, 0, 6000, 6000), 3.0);

        Assert.True(plan.WasScaleReduced);
        Assert.True(plan.PixelCount <= ExportSettings.MaxPixelCount);
        Assert.True(plan.PixelWidth <= ExportSettings.MaxDimension);
    }

    [Fact]
    public void ClampingNeverProducesAnEmptyBitmap()
    {
        var plan = Plan(new RectD(0, 0, 200_000, 1), 3.0);

        Assert.True(plan.PixelWidth >= 1);
        Assert.True(plan.PixelHeight >= 1);
    }

    [Fact]
    public void DegenerateRegion_StillProducesAValidPlan()
    {
        var plan = Plan(new RectD(10, 10, 0, 0), 2.0);

        Assert.True(plan.PixelWidth >= 1);
        Assert.True(plan.PixelHeight >= 1);
    }

    [Fact]
    public void PlanDoesNotDependOnAnyScreenScaling()
    {
        // The math has no notion of monitor DPI: the same inputs always yield the
        // same file size.
        var first = Plan(new RectD(0, 0, 640, 480), 2.0, margin: 24);
        var second = Plan(new RectD(0, 0, 640, 480), 2.0, margin: 24);

        Assert.Equal(first, second);
        Assert.Equal(1328, first.PixelWidth);
        Assert.Equal(1008, first.PixelHeight);
    }

    [Fact]
    public void InvalidScaleOrMargin_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Plan(new RectD(0, 0, 10, 10), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Plan(new RectD(0, 0, 10, 10), 1, -1));
    }
}
