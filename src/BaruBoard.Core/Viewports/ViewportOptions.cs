namespace BaruBoard.Core.Viewports;

public sealed record ViewportOptions
{
    public double MinZoom { get; init; } = 0.1;

    public double MaxZoom { get; init; } = 8.0;

    public double ZoomStepFactor { get; init; } = 1.1;
}
