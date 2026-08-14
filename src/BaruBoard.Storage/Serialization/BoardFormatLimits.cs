namespace BaruBoard.Storage.Serialization;

/// <summary>
/// Ceilings applied while reading a board file. They exist to keep a hostile or
/// damaged file from exhausting memory and disk, not to constrain real boards.
/// </summary>
public static class BoardFormatLimits
{
    public const int MaxContainerEntries = 4096;

    public const int MaxBoardJsonBytes = 64 * 1024 * 1024;

    public const int MaxAssets = 512;

    public const int MaxAssetBytes = 64 * 1024 * 1024;

    public const long MaxTotalUncompressedBytes = 512L * 1024 * 1024;

    public const int MaxElements = 100_000;
}
