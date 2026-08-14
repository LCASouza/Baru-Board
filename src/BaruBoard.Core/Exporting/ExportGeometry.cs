using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Exporting;

/// <summary>
/// What an export will actually produce. <see cref="EffectiveScale"/> may be
/// lower than what was asked for when the limits kick in.
/// </summary>
public readonly record struct ExportPlan(
    RectD WorldRegion,
    double RequestedScale,
    double EffectiveScale,
    int PixelWidth,
    int PixelHeight)
{
    public bool WasScaleReduced => EffectiveScale < RequestedScale - 1e-9;

    public long PixelCount => (long)PixelWidth * PixelHeight;
}

/// <summary>
/// Pure world-region to output-pixels math. One world unit maps to
/// <c>scale</c> output pixels regardless of the monitor the window sits on.
/// </summary>
public static class ExportGeometry
{
    private const double MinimumExtent = 1.0;

    public static ExportPlan CreatePlan(RectD worldRegion, double requestedScale, double marginPixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedScale);
        ArgumentOutOfRangeException.ThrowIfNegative(marginPixels);

        // The margin is defined in output pixels, so it is converted with the
        // requested scale before the limits possibly lower it.
        var region = Inflate(worldRegion, marginPixels / requestedScale);
        var width = Math.Max(region.Width, MinimumExtent);
        var height = Math.Max(region.Height, MinimumExtent);

        var scale = requestedScale;
        scale = Math.Min(scale, ExportSettings.MaxDimension / width);
        scale = Math.Min(scale, ExportSettings.MaxDimension / height);

        var pixelBudgetScale = Math.Sqrt(ExportSettings.MaxPixelCount / (width * height));
        scale = Math.Min(scale, pixelBudgetScale);

        var pixelWidth = ToPixels(width, scale);
        var pixelHeight = ToPixels(height, scale);

        return new ExportPlan(
            new RectD(region.X, region.Y, width, height),
            requestedScale,
            scale,
            pixelWidth,
            pixelHeight);
    }

    public static RectD Inflate(RectD region, double amount) => new(
        region.X - amount,
        region.Y - amount,
        Math.Max(region.Width + amount * 2, 0),
        Math.Max(region.Height + amount * 2, 0));

    // Rounding down keeps the pixel budget an actual guarantee: rounding up on
    // both axes can push the product past the limit it is meant to enforce.
    private static int ToPixels(double worldExtent, double scale) =>
        Math.Clamp((int)Math.Floor(worldExtent * scale + 1e-9), 1, ExportSettings.MaxDimension);
}
