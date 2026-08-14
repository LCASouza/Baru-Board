namespace BaruBoard.Core.Boards;

public static class AssetMediaTypes
{
    public const string Png = "image/png";

    public const string Jpeg = "image/jpeg";

    public const string Webp = "image/webp";

    public static bool IsSupported(string? mediaType) =>
        mediaType is Png or Jpeg or Webp;

    public static string GetExtension(string mediaType) => mediaType switch
    {
        Png => ".png",
        Jpeg => ".jpg",
        Webp => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType)),
    };

    public static string? FromFileExtension(string? extension) => extension?.ToLowerInvariant() switch
    {
        ".png" => Png,
        ".jpg" or ".jpeg" => Jpeg,
        ".webp" => Webp,
        _ => null,
    };
}
