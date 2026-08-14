using Avalonia.Media;
using BaruBoard.Core.Boards;

namespace BaruBoard.App.Rendering;

/// <summary>
/// Supplies the decoded bitmap for an image element. The screen and an export
/// need different resolutions from the same asset bytes, so the renderer asks
/// for what it needs and stays out of the caching policy.
/// </summary>
public interface IImageBitmapProvider
{
    /// <param name="outputScale">Output pixels per world unit of this pass.</param>
    IImage? GetImage(BoardDocument document, ImageElement element, double outputScale);
}
