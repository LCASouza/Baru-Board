using BaruBoard.Core.Geometry;

namespace BaruBoard.Core.Viewports;

/// <summary>
/// Camera framing math, kept pure so it can be tested without any UI. None of it
/// touches the document, the history or the dirty state.
/// </summary>
public static class ViewportFraming
{
    public const double DefaultPaddingDips = 48.0;

    // A degenerate region (a single point, a zero-height line) still needs some
    // extent to be framed against.
    private const double MinimumExtent = 1.0;

    public static (PointD Position, double Zoom)? FitToContent(
        RectD content, SizeD viewportSize, double padding, double minZoom, double maxZoom)
    {
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0)
            return null;

        var available = new SizeD(
            Math.Max(viewportSize.Width - padding * 2, viewportSize.Width / 2),
            Math.Max(viewportSize.Height - padding * 2, viewportSize.Height / 2));

        var width = Math.Max(content.Width, MinimumExtent);
        var height = Math.Max(content.Height, MinimumExtent);

        var zoom = Math.Clamp(Math.Min(available.Width / width, available.Height / height), minZoom, maxZoom);

        var center = new PointD(content.X + content.Width / 2, content.Y + content.Height / 2);
        var position = new PointD(
            center.X - viewportSize.Width / 2 / zoom,
            center.Y - viewportSize.Height / 2 / zoom);

        return (position, zoom);
    }
}
