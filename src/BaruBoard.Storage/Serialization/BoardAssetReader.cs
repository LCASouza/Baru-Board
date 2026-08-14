using BaruBoard.Core.Boards;

namespace BaruBoard.Storage.Serialization;

internal static class BoardAssetReader
{
    /// <summary>
    /// Rebuilds an asset from untrusted bytes. The declared id is only accepted
    /// once it matches the hash of the content that was actually read.
    /// </summary>
    public static BoardAsset Create(string? declaredId, string? mediaType, ReadOnlySpan<byte> data)
    {
        if (!BoardAsset.IsValidId(declaredId))
            throw new BoardFormatException($"Asset id '{declaredId}' is not a lowercase SHA-256 hex string.");

        if (!AssetMediaTypes.IsSupported(mediaType))
            throw new BoardFormatException($"Asset '{declaredId}' has unsupported media type '{mediaType}'.");

        var asset = BoardAsset.Create(data, mediaType!);
        if (!string.Equals(asset.Id, declaredId, StringComparison.Ordinal))
            throw new BoardFormatException($"Asset '{declaredId}' does not match the hash of its content.");

        return asset;
    }

    public static void ValidateManifestEntry(AssetDto? dto)
    {
        FormatGuard.NotNull(dto, "assets[]");
        if (!BoardAsset.IsValidId(dto!.Id))
            throw new BoardFormatException($"Asset id '{dto.Id}' is not a lowercase SHA-256 hex string.");

        if (!AssetMediaTypes.IsSupported(dto.MediaType))
            throw new BoardFormatException($"Asset '{dto.Id}' has unsupported media type '{dto.MediaType}'.");
    }
}
