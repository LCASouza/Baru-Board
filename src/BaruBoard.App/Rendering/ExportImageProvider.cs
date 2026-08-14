using Avalonia.Media;
using Avalonia.Media.Imaging;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Exporting;

namespace BaruBoard.App.Rendering;

/// <summary>
/// Decodes assets at the resolution the exported file actually needs, instead of
/// reusing the capped preview bitmaps kept for the screen. Lives only for the
/// duration of one export.
/// </summary>
public sealed class ExportImageProvider : IImageBitmapProvider, IDisposable
{
    private readonly Dictionary<string, Bitmap?> _bitmaps = new(StringComparer.Ordinal);

    public IImage? GetImage(BoardDocument document, ImageElement element, double outputScale)
    {
        var targetWidth = Math.Clamp(
            (int)Math.Ceiling(element.Bounds.Width * outputScale),
            1,
            ExportSettings.MaxDimension);

        // One entry per asset is enough: the same asset drawn twice in an export
        // is drawn at the same scale.
        var key = $"{element.AssetId}:{targetWidth}";
        if (_bitmaps.TryGetValue(key, out var cached))
            return cached;

        Bitmap? bitmap = null;
        if (document.TryGetAsset(element.AssetId, out var asset))
        {
            try
            {
                using var stream = asset.OpenRead();
                bitmap = Bitmap.DecodeToWidth(stream, targetWidth);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                bitmap = null;
            }
        }

        _bitmaps[key] = bitmap;
        return bitmap;
    }

    public void Dispose()
    {
        foreach (var bitmap in _bitmaps.Values)
            bitmap?.Dispose();

        _bitmaps.Clear();
    }
}
