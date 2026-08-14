namespace BaruBoard.Core.Exporting;

public enum ExportRegionKind
{
    Content,
    Selection,
    VisibleArea,
}

public static class ExportSettings
{
    /// <summary>
    /// Border added around content and selection exports, expressed in output
    /// pixels so the visual margin looks the same at every scale.
    /// </summary>
    public const double MarginPixels = 24.0;

    public const int MaxDimension = 8192;

    // A full RGBA buffer of this many pixels already takes around 100 MB, which
    // is the practical ceiling for a single export.
    public const long MaxPixelCount = 28_000_000;

    public static readonly double[] Scales = [1.0, 2.0, 3.0];
}
