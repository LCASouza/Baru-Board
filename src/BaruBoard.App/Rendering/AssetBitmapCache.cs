using Avalonia.Media;
using Avalonia.Media.Imaging;
using BaruBoard.Core.Boards;

namespace BaruBoard.App.Rendering;

/// <summary>
/// Keeps one decoded bitmap per asset for the lifetime of a document. Decoding is
/// capped on the largest dimension so a photo cannot blow up memory; the asset
/// bytes themselves are never altered.
/// </summary>
public sealed class AssetBitmapCache : IImageBitmapProvider, IDisposable
{
    public const int MaxDecodedDimension = 2048;

    private readonly Dictionary<string, Bitmap?> _bitmaps = new(StringComparer.Ordinal);

    // The screen never needs more than the capped preview, so the requested
    // output scale is deliberately ignored here.
    public IImage? GetImage(BoardDocument document, ImageElement element, double outputScale) =>
        Get(document, element.AssetId);

    public Bitmap? Get(BoardDocument document, string assetId)
    {
        if (_bitmaps.TryGetValue(assetId, out var cached))
            return cached;

        Bitmap? bitmap = null;
        if (document.TryGetAsset(assetId, out var asset))
            bitmap = TryDecode(asset);

        // Failures are cached too, otherwise a broken asset is retried every frame.
        _bitmaps[assetId] = bitmap;
        return bitmap;
    }

    public void Clear()
    {
        foreach (var bitmap in _bitmaps.Values)
            bitmap?.Dispose();

        _bitmaps.Clear();
    }

    public void Dispose() => Clear();

    private static Bitmap? TryDecode(BoardAsset asset)
    {
        try
        {
            using var stream = asset.OpenRead();
            var bitmap = new Bitmap(stream);
            var size = bitmap.PixelSize;
            if (Math.Max(size.Width, size.Height) <= MaxDecodedDimension)
                return bitmap;

            bitmap.Dispose();
            stream.Position = 0;
            return size.Width >= size.Height
                ? Bitmap.DecodeToWidth(stream, MaxDecodedDimension)
                : Bitmap.DecodeToHeight(stream, MaxDecodedDimension);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }
}
