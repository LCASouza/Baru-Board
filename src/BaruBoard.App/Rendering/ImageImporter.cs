using Avalonia.Media.Imaging;
using BaruBoard.Core.Boards;
using BaruBoard.Core.Geometry;

namespace BaruBoard.App.Rendering;

public sealed record ImportedImage(BoardAsset Asset, SizeD InitialSize);

internal static class ImageImporter
{
    // World-space cap for a freshly imported image; smaller pictures keep their
    // intrinsic size instead of being blown up to reach it.
    public const double MaxInitialSize = 800.0;

    public static async Task<ImportedImage?> TryLoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var mediaType = AssetMediaTypes.FromFileExtension(Path.GetExtension(path));
        if (mediaType is null)
            return null;

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        return await Task.Run(
            () =>
            {
                try
                {
                    using var stream = new MemoryStream(bytes, writable: false);
                    using var bitmap = new Bitmap(stream);
                    var size = GetInitialSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
                    return new ImportedImage(BoardAsset.Create(bytes, mediaType), size);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    return null;
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    public static SizeD GetInitialSize(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
            return new SizeD(MaxInitialSize, MaxInitialSize);

        var scale = Math.Min(1.0, MaxInitialSize / Math.Max(pixelWidth, pixelHeight));
        return new SizeD(pixelWidth * scale, pixelHeight * scale);
    }
}
